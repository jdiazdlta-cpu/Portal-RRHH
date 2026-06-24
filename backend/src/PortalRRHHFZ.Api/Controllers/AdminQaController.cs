using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PortalRRHHFZ.Application.Common;
using PortalRRHHFZ.Application.DTOs;
using PortalRRHHFZ.Domain.Constants;
using PortalRRHHFZ.Domain.Entities;
using PortalRRHHFZ.Domain.Enums;
using PortalRRHHFZ.Infrastructure.Data;

namespace PortalRRHHFZ.Api.Controllers;

[ApiController]
[Authorize(Policy = AppPolicies.RequireAdmin)]
[Route("api/admin/qa")]
public sealed class AdminQaController(AppDbContext db) : ControllerBase
{
    private static readonly DateTime QaRecentThreshold = new(2026, 6, 23);
    private const string ProtectedOrganigramaReason = "Organigrama activo o posiblemente real. No se recomienda eliminar.";
    private const string ProtectedNodoReason = "Nodo activo o asociado a un organigrama protegido. No se recomienda eliminar.";
    private const string ProtectedResponsableReason = "Responsable activo o asociado a una estructura protegida. No se recomienda eliminar.";
    private static readonly string[] QaTerms = ["QA", "TEST", "PRUEBA", "CODEX"];
    private static readonly HashSet<string> KnownQaSolicitudCodes = new(StringComparer.OrdinalIgnoreCase)
    {
        "AP-2026-000001",
        "AP-2026-000002",
        "AP-2026-000003",
        "AP-2026-000004",
        "AP-2026-000005",
        "AP-2026-000006",
        "AP-2026-000007",
        "REQ-2026-000007",
        "REQ-2026-000008"
    };
    private static readonly HashSet<int> KnownQaNodeIds = [13, 14];

    [HttpGet("inventario")]
    public async Task<IActionResult> Inventario(CancellationToken cancellationToken)
    {
        var inventory = await BuildInventoryAsync(cancellationToken);
        return Ok(ApiResponse<QaInventoryDto>.Ok(inventory));
    }

    [HttpPost("limpiar")]
    public async Task<IActionResult> Limpiar([FromBody] QaCleanupRequest request, CancellationToken cancellationToken)
    {
        if (!request.Confirmar)
        {
            return BadRequest(ApiResponse<object>.Fail("Debe confirmar explicitamente la limpieza QA."));
        }

        var inventory = await BuildInventoryAsync(cancellationToken);
        var invalid = new List<string>();
        ValidateCleanupSelection(request.SolicitudIds, inventory.Solicitudes, "Solicitud", invalid);
        ValidateCleanupSelection(request.OrganigramaIds, inventory.Organigramas, "Organigrama", invalid);
        ValidateCleanupSelection(request.NodoIds, inventory.Nodos, "Nodo", invalid);
        ValidateCleanupSelection(request.ResponsableIds, inventory.Responsables, "Responsable", invalid);

        if (invalid.Count > 0)
        {
            return BadRequest(ApiResponse<object>.Fail($"No se ejecuto la limpieza QA. {string.Join(" ", invalid)}"));
        }

        var solicitudIds = request.SolicitudIds.Distinct().ToList();
        var organigramaIds = request.OrganigramaIds.Distinct().ToList();
        var nodoIds = request.NodoIds.Distinct().ToList();
        var responsableIds = request.ResponsableIds.Distinct().ToList();
        var warnings = new List<string>();

        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);

        var cleanupSolicitudes = await DeleteSolicitudesAsync(solicitudIds, warnings, cancellationToken);
        var cleanupOrganigrama = await DeleteOrganigramaAsync(organigramaIds, nodoIds, responsableIds, cancellationToken);

        await transaction.CommitAsync(cancellationToken);

        var result = new QaCleanupResultDto(
            cleanupSolicitudes.Solicitudes,
            cleanupSolicitudes.Requisiciones,
            cleanupSolicitudes.AccionesPersonal,
            cleanupSolicitudes.Cambios,
            cleanupSolicitudes.Aprobaciones,
            cleanupSolicitudes.Historial,
            cleanupOrganigrama.Organigramas,
            cleanupOrganigrama.Nodos,
            cleanupOrganigrama.Responsables,
            cleanupOrganigrama.Historial,
            warnings);

        return Ok(ApiResponse<QaCleanupResultDto>.Ok(result, "Limpieza QA ejecutada."));
    }

    private async Task<QaInventoryDto> BuildInventoryAsync(CancellationToken cancellationToken)
    {
        var solicitudes = await db.Solicitudes
            .Include(x => x.RequisicionPersonal)
            .Include(x => x.AccionPersonal)
            .AsNoTracking()
            .OrderByDescending(x => x.FechaSolicitud)
            .ToListAsync(cancellationToken);

        var solicitudItems = new List<QaInventoryItemDto>();
        var requisicionItems = new List<QaInventoryItemDto>();
        var accionItems = new List<QaInventoryItemDto>();
        var alertaItems = new List<QaInventoryItemDto>();

        foreach (var solicitud in solicitudes)
        {
            var motivos = DetectSolicitudMotivos(solicitud);
            if (motivos.Count == 0)
            {
                continue;
            }

            var safe = IsSafeSolicitud(solicitud, motivos);
            var riesgo = safe ? "Bajo" : "Requiere revision";
            solicitudItems.Add(new QaInventoryItemDto(
                "Solicitud",
                solicitud.SolicitudId,
                solicitud.CodigoSolicitud,
                solicitud.TipoSolicitud.ToString(),
                solicitud.Estado.ToString(),
                solicitud.IsActive,
                solicitud.CreatedAt,
                solicitud.CreatedBy,
                string.Join("; ", motivos),
                safe,
                riesgo));

            if (solicitud.RequisicionPersonal is not null)
            {
                requisicionItems.Add(new QaInventoryItemDto(
                    "RequisicionPersonal",
                    solicitud.RequisicionPersonal.RequisicionPersonalId,
                    solicitud.CodigoSolicitud,
                    solicitud.RequisicionPersonal.CargoSolicitado,
                    solicitud.Estado.ToString(),
                    solicitud.IsActive,
                    solicitud.CreatedAt,
                    solicitud.CreatedBy,
                    string.Join("; ", motivos),
                    safe,
                    riesgo));
            }

            if (solicitud.AccionPersonal is not null)
            {
                accionItems.Add(new QaInventoryItemDto(
                    "AccionPersonal",
                    solicitud.AccionPersonal.AccionPersonalId,
                    solicitud.CodigoSolicitud,
                    solicitud.AccionPersonal.TipoAccion.ToString(),
                    solicitud.Estado.ToString(),
                    solicitud.IsActive,
                    solicitud.CreatedAt,
                    solicitud.CreatedBy,
                    string.Join("; ", motivos),
                    safe,
                    solicitud.AccionPersonal.Ejecutada ? "Alto: accion ejecutada" : riesgo));

                if (solicitud.AccionPersonal.AlertaOrigenId.HasValue)
                {
                    alertaItems.Add(new QaInventoryItemDto(
                        "AlertaRelacionada",
                        solicitud.AccionPersonal.AlertaOrigenId.Value,
                        solicitud.CodigoSolicitud,
                        "Alerta origen de accion QA",
                        solicitud.Estado.ToString(),
                        null,
                        solicitud.CreatedAt,
                        solicitud.CreatedBy,
                        "Alerta relacionada con una accion de personal detectada como QA; no se borra automaticamente.",
                        false,
                        "No borrar alerta real"));
                }
            }
        }

        var organigramaInventory = await BuildOrganigramaInventoryAsync(cancellationToken);
        var organigramas = organigramaInventory.Items;
        var nodos = await BuildNodoInventoryAsync(organigramaInventory.Protection, cancellationToken);
        var responsables = await BuildResponsableInventoryAsync(organigramaInventory.Protection, cancellationToken);
        var total = solicitudItems.Count + requisicionItems.Count + accionItems.Count + organigramas.Count + nodos.Count + responsables.Count + alertaItems.Count;

        return new QaInventoryDto(
            solicitudItems,
            requisicionItems,
            accionItems,
            organigramas,
            nodos,
            responsables,
            alertaItems
                .GroupBy(x => x.Id)
                .Select(x => x.First())
                .OrderBy(x => x.Id)
                .ToList(),
            total,
            DateTime.UtcNow);
    }

    private async Task<OrganigramaInventoryResult> BuildOrganigramaInventoryAsync(CancellationToken cancellationToken)
    {
        var activeResponsibles = await db.DepartamentoResponsables
            .Where(x => x.IsActive)
            .Select(x => new ResponsibleScope(x.EmpresaId, x.DepartamentoId))
            .ToListAsync(cancellationToken);

        var organigramas = await db.Organigramas
            .Include(x => x.Nodos)
            .AsNoTracking()
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync(cancellationToken);

        var protectedOrganigramaIds = new HashSet<int>();
        var protectedEmpresaIds = new HashSet<int>();
        var protectedDepartamentoIds = new HashSet<int>();
        var protectedNodeIds = new HashSet<int>();
        var items = organigramas
            .Select(org =>
            {
                var motivos = new List<string>();
                var hasQaText = ContainsQaText(org.Nombre, org.Descripcion, org.CreatedBy);
                var hasActiveNodes = org.Nodos.Any(x => x.IsActive);
                var hasActiveResponsibles = activeResponsibles.Any(scope => MatchesOrganigramaScope(org, scope));
                if (org.IsActive) motivos.Add("Organigrama activo");
                if (!org.IsActive) motivos.Add("Organigrama inactivo");
                if (hasActiveNodes) motivos.Add("Tiene nodos activos");
                if (hasActiveResponsibles) motivos.Add("Tiene responsables activos asociados");
                if (hasQaText) motivos.Add("Texto contiene QA/TEST/PRUEBA/CODEX");
                if (org.CreatedAt >= QaRecentThreshold) motivos.Add("Creado recientemente; requiere revision");
                var safe = !org.IsActive && !hasActiveNodes && !hasActiveResponsibles && hasQaText;
                var protectedItem = !safe;
                if (protectedItem)
                {
                    protectedOrganigramaIds.Add(org.OrganigramaId);
                    if (org.EmpresaId.HasValue) protectedEmpresaIds.Add(org.EmpresaId.Value);
                    foreach (var node in org.Nodos)
                    {
                        protectedNodeIds.Add(node.OrganigramaNodoId);
                        if (node.EmpresaId.HasValue) protectedEmpresaIds.Add(node.EmpresaId.Value);
                        if (node.DepartamentoId.HasValue) protectedDepartamentoIds.Add(node.DepartamentoId.Value);
                    }
                }

                return new QaInventoryItemDto(
                    "Organigrama",
                    org.OrganigramaId,
                    null,
                    org.Nombre,
                    org.IsActive ? "Activo" : "Inactivo",
                    org.IsActive,
                    org.CreatedAt,
                    org.CreatedBy,
                    string.Join("; ", motivos),
                    safe,
                    safe ? "Bajo" : "Protegido",
                    protectedItem,
                    protectedItem ? ProtectedOrganigramaReason : null);
            })
            .Where(x => !string.IsNullOrWhiteSpace(x.MotivoDeteccion))
            .ToList();

        return new OrganigramaInventoryResult(
            items,
            new QaProtectionScope(protectedOrganigramaIds, protectedNodeIds, protectedEmpresaIds, protectedDepartamentoIds));
    }

    private async Task<List<QaInventoryItemDto>> BuildNodoInventoryAsync(QaProtectionScope protection, CancellationToken cancellationToken)
    {
        var childCounts = await db.OrganigramaNodos
            .Where(x => x.NodoPadreId.HasValue)
            .GroupBy(x => x.NodoPadreId!.Value)
            .Select(x => new { NodoId = x.Key, Count = x.Count() })
            .ToDictionaryAsync(x => x.NodoId, x => x.Count, cancellationToken);

        var nodos = await db.OrganigramaNodos
            .AsNoTracking()
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync(cancellationToken);

        return nodos
            .Select(node =>
            {
                var motivos = new List<string>();
                var protectedItem = node.IsActive || protection.OrganigramaIds.Contains(node.OrganigramaId) || protection.NodoIds.Contains(node.OrganigramaNodoId);
                if (KnownQaNodeIds.Contains(node.OrganigramaNodoId)) motivos.Add("ID conocido de prueba");
                if (node.IsActive) motivos.Add("Nodo activo");
                if (!node.IsActive) motivos.Add("Nodo inactivo");
                if (ContainsQaText(node.NombreNodo, node.Descripcion, node.CreatedBy)) motivos.Add("Texto contiene QA/TEST/PRUEBA/CODEX");
                if (node.CreatedAt >= QaRecentThreshold) motivos.Add("Creado recientemente; requiere revision");
                if (protectedItem && !node.IsActive) motivos.Add("Pertenece a organigrama protegido");
                var safe = !node.IsActive &&
                    !protectedItem &&
                    !childCounts.ContainsKey(node.OrganigramaNodoId) &&
                    (KnownQaNodeIds.Contains(node.OrganigramaNodoId) || ContainsQaText(node.NombreNodo, node.Descripcion, node.CreatedBy));

                return new QaInventoryItemDto(
                    "OrganigramaNodo",
                    node.OrganigramaNodoId,
                    null,
                    node.NombreNodo,
                    node.IsActive ? "Activo" : "Inactivo",
                    node.IsActive,
                    node.CreatedAt,
                    node.CreatedBy,
                    string.Join("; ", motivos),
                    safe,
                    safe ? "Bajo" : protectedItem ? "Protegido" : childCounts.ContainsKey(node.OrganigramaNodoId) ? "Medio: tiene nodos hijos" : "Requiere revision",
                    protectedItem,
                    protectedItem ? ProtectedNodoReason : null);
            })
            .Where(x => !string.IsNullOrWhiteSpace(x.MotivoDeteccion))
            .ToList();
    }

    private async Task<List<QaInventoryItemDto>> BuildResponsableInventoryAsync(QaProtectionScope protection, CancellationToken cancellationToken)
    {
        var referenced = await db.SolicitudAprobaciones
            .Where(x => x.DepartamentoResponsableId.HasValue)
            .Select(x => x.DepartamentoResponsableId!.Value)
            .Distinct()
            .ToListAsync(cancellationToken);
        var referencedSet = referenced.ToHashSet();

        var responsables = await db.DepartamentoResponsables
            .Include(x => x.ColaboradorResponsable)
            .AsNoTracking()
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync(cancellationToken);

        return responsables
            .Select(responsable =>
            {
                var motivos = new List<string>();
                var hasQaText = ContainsQaText(responsable.TipoResponsable, responsable.Observacion, responsable.CreatedBy);
                var belongsToProtectedScope =
                    protection.DepartamentoIds.Contains(responsable.DepartamentoId) ||
                    protection.EmpresaIds.Contains(responsable.EmpresaId);
                var protectedItem = responsable.IsActive || (belongsToProtectedScope && !hasQaText);
                if (responsable.IsActive) motivos.Add("Responsable activo");
                if (!responsable.IsActive) motivos.Add("Responsable inactivo");
                if (hasQaText) motivos.Add("Texto contiene QA/TEST/PRUEBA/CODEX");
                if (responsable.CreatedAt >= QaRecentThreshold) motivos.Add("Creado recientemente; requiere revision");
                if (protectedItem && belongsToProtectedScope) motivos.Add("Asociado a empresa/departamento usado por organigrama protegido");
                var safe = !protectedItem && !responsable.IsActive && !referencedSet.Contains(responsable.DepartamentoResponsableId) && hasQaText;

                return new QaInventoryItemDto(
                    "DepartamentoResponsable",
                    responsable.DepartamentoResponsableId,
                    null,
                    responsable.ColaboradorResponsable.NombreCompleto(),
                    responsable.IsActive ? "Activo" : "Inactivo",
                    responsable.IsActive,
                    responsable.CreatedAt,
                    responsable.CreatedBy,
                    string.Join("; ", motivos),
                    safe,
                    safe ? "Bajo" : protectedItem ? "Protegido" : referencedSet.Contains(responsable.DepartamentoResponsableId) ? "Medio: usado en aprobaciones" : "Requiere revision",
                    protectedItem,
                    protectedItem ? ProtectedResponsableReason : null);
            })
            .Where(x => !string.IsNullOrWhiteSpace(x.MotivoDeteccion))
            .ToList();
    }

    private async Task<(int Solicitudes, int Requisiciones, int AccionesPersonal, int Cambios, int Aprobaciones, int Historial)> DeleteSolicitudesAsync(
        IReadOnlyCollection<int> solicitudIds,
        List<string> warnings,
        CancellationToken cancellationToken)
    {
        if (solicitudIds.Count == 0)
        {
            return (0, 0, 0, 0, 0, 0);
        }

        var accionIds = await db.AccionesPersonal
            .Where(x => solicitudIds.Contains(x.SolicitudId))
            .Select(x => x.AccionPersonalId)
            .ToListAsync(cancellationToken);

        var accionesDesdeAlerta = await db.AccionesPersonal
            .Where(x => solicitudIds.Contains(x.SolicitudId) && x.AlertaOrigenId.HasValue)
            .CountAsync(cancellationToken);
        if (accionesDesdeAlerta > 0)
        {
            warnings.Add("Se borraron acciones de personal QA relacionadas con alertas; las alertas no se borraron ni se cerraron.");
        }

        var cambios = accionIds.Count == 0
            ? 0
            : await db.AccionPersonalCambiosAplicados.Where(x => accionIds.Contains(x.AccionPersonalId)).ExecuteDeleteAsync(cancellationToken);
        var acciones = await db.AccionesPersonal.Where(x => solicitudIds.Contains(x.SolicitudId)).ExecuteDeleteAsync(cancellationToken);
        var requisiciones = await db.RequisicionesPersonal.Where(x => solicitudIds.Contains(x.SolicitudId)).ExecuteDeleteAsync(cancellationToken);
        var aprobaciones = await db.SolicitudAprobaciones.Where(x => solicitudIds.Contains(x.SolicitudId)).ExecuteDeleteAsync(cancellationToken);
        var historial = await db.SolicitudHistorial.Where(x => solicitudIds.Contains(x.SolicitudId)).ExecuteDeleteAsync(cancellationToken);
        var solicitudes = await db.Solicitudes.Where(x => solicitudIds.Contains(x.SolicitudId)).ExecuteDeleteAsync(cancellationToken);

        return (solicitudes, requisiciones, acciones, cambios, aprobaciones, historial);
    }

    private async Task<(int Organigramas, int Nodos, int Responsables, int Historial)> DeleteOrganigramaAsync(
        IReadOnlyCollection<int> organigramaIds,
        IReadOnlyCollection<int> nodoIds,
        IReadOnlyCollection<int> responsableIds,
        CancellationToken cancellationToken)
    {
        var nodesFromOrganigramas = organigramaIds.Count == 0
            ? new List<int>()
            : await db.OrganigramaNodos
                .Where(x => organigramaIds.Contains(x.OrganigramaId))
                .Select(x => x.OrganigramaNodoId)
                .ToListAsync(cancellationToken);
        var allNodeIds = nodoIds.Concat(nodesFromOrganigramas).Distinct().ToList();

        var historial = 0;
        if (allNodeIds.Count > 0)
        {
            historial += await db.OrganigramaHistorialCambios
                .Where(x => x.Entidad == nameof(OrganigramaNodo) && allNodeIds.Contains(x.EntidadId))
                .ExecuteDeleteAsync(cancellationToken);
        }

        if (responsableIds.Count > 0)
        {
            historial += await db.OrganigramaHistorialCambios
                .Where(x => x.Entidad == nameof(DepartamentoResponsable) && responsableIds.Contains(x.EntidadId))
                .ExecuteDeleteAsync(cancellationToken);
        }

        if (organigramaIds.Count > 0)
        {
            historial += await db.OrganigramaHistorialCambios
                .Where(x => x.Entidad == nameof(Organigrama) && organigramaIds.Contains(x.EntidadId))
                .ExecuteDeleteAsync(cancellationToken);
        }

        var nodosBorrados = 0;
        if (allNodeIds.Count > 0)
        {
            var nodes = await db.OrganigramaNodos
                .Where(x => allNodeIds.Contains(x.OrganigramaNodoId))
                .OrderByDescending(x => x.Nivel)
                .ToListAsync(cancellationToken);
            db.OrganigramaNodos.RemoveRange(nodes);
            nodosBorrados = nodes.Count;
            await db.SaveChangesAsync(cancellationToken);
        }

        var responsables = responsableIds.Count == 0
            ? 0
            : await db.DepartamentoResponsables.Where(x => responsableIds.Contains(x.DepartamentoResponsableId)).ExecuteDeleteAsync(cancellationToken);

        var organigramas = organigramaIds.Count == 0
            ? 0
            : await db.Organigramas.Where(x => organigramaIds.Contains(x.OrganigramaId)).ExecuteDeleteAsync(cancellationToken);

        return (organigramas, nodosBorrados, responsables, historial);
    }

    private static List<string> DetectSolicitudMotivos(Solicitud solicitud)
    {
        var motivos = new List<string>();
        if (KnownQaSolicitudCodes.Contains(solicitud.CodigoSolicitud))
        {
            motivos.Add("Codigo conocido de prueba");
        }

        if (ContainsQaText(
            solicitud.Justificacion,
            solicitud.Observaciones,
            solicitud.CreatedBy,
            solicitud.RequisicionPersonal?.CargoSolicitado,
            solicitud.RequisicionPersonal?.NombrePersonaReemplazada,
            solicitud.RequisicionPersonal?.CentroTrabajo,
            solicitud.AccionPersonal?.Justificacion,
            solicitud.AccionPersonal?.Observaciones))
        {
            motivos.Add("Texto contiene QA/TEST/PRUEBA/CODEX");
        }

        if (solicitud.CreatedAt >= QaRecentThreshold && solicitud.Estado == EstadoSolicitud.Borrador)
        {
            motivos.Add("Borrador creado recientemente; requiere revision");
        }

        return motivos;
    }

    private static bool IsSafeSolicitud(Solicitud solicitud, IReadOnlyCollection<string> motivos)
    {
        if (solicitud.AccionPersonal?.Ejecutada == true)
        {
            return false;
        }

        var hasStrongSignal = motivos.Any(x => x.Contains("Codigo conocido", StringComparison.OrdinalIgnoreCase) || x.Contains("Texto contiene", StringComparison.OrdinalIgnoreCase));
        return hasStrongSignal &&
            (solicitud.Estado is EstadoSolicitud.Borrador or EstadoSolicitud.Devuelta or EstadoSolicitud.Cancelada or EstadoSolicitud.Rechazada or EstadoSolicitud.Aprobada);
    }

    private static void ValidateCleanupSelection(IEnumerable<int> requestedIds, IEnumerable<QaInventoryItemDto> inventoryItems, string label, List<string> invalid)
    {
        var itemsById = inventoryItems
            .GroupBy(x => x.Id)
            .ToDictionary(x => x.Key, x => x.First());

        foreach (var id in requestedIds.Distinct())
        {
            if (!itemsById.TryGetValue(id, out var item))
            {
                invalid.Add($"{label} {id}: no esta detectado como QA seguro.");
                continue;
            }

            if (item.EsProtegido)
            {
                invalid.Add($"{label} {id}: protegido. {item.MotivoProteccion ?? "No se elimino."}");
                continue;
            }

            if (!item.PuedeBorrarseSeguro)
            {
                invalid.Add($"{label} {id}: no cumple criterios seguros de borrado.");
            }
        }
    }

    private static bool MatchesOrganigramaScope(Organigrama organigrama, ResponsibleScope scope)
    {
        return organigrama.Nodos.Any(node =>
            node.DepartamentoId == scope.DepartamentoId ||
            (!node.DepartamentoId.HasValue && node.EmpresaId == scope.EmpresaId));
    }

    private static bool ContainsQaText(params string?[] values)
    {
        return values
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Any(value => QaTerms.Any(term => value!.Contains(term, StringComparison.OrdinalIgnoreCase)));
    }

    private sealed record ResponsibleScope(int EmpresaId, int DepartamentoId);

    private sealed record QaProtectionScope(
        HashSet<int> OrganigramaIds,
        HashSet<int> NodoIds,
        HashSet<int> EmpresaIds,
        HashSet<int> DepartamentoIds);

    private sealed record OrganigramaInventoryResult(List<QaInventoryItemDto> Items, QaProtectionScope Protection);
}
