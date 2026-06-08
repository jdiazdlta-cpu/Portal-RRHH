using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using PortalRRHHFZ.Application.Common;
using PortalRRHHFZ.Application.DTOs.Alertas;
using PortalRRHHFZ.Application.Interfaces.Alertas;
using PortalRRHHFZ.Domain.Entities;
using PortalRRHHFZ.Domain.Enums;
using PortalRRHHFZ.Infrastructure.Data;

namespace PortalRRHHFZ.Infrastructure.Services;

public sealed class AlertaService(AppDbContext dbContext) : IAlertaService
{
    private const int DiasAnticipacion = 7;
    private static readonly string[] EstatusOperativos = ["A", "V", "S"];

    public async Task<ApiResponse<IReadOnlyCollection<AlertaListDto>>> GetAllAsync(
        AlertaFilterRequest filters,
        CancellationToken cancellationToken = default)
    {
        var query = BaseQuery().AsNoTracking();

        if (!filters.IncluirInactivas)
        {
            query = query.Where(alerta => alerta.IsActive);
        }

        if (filters.EstadoAlerta.HasValue)
        {
            query = query.Where(alerta => alerta.EstadoAlerta == filters.EstadoAlerta.Value);
        }

        if (filters.TipoAlerta.HasValue)
        {
            query = query.Where(alerta => alerta.TipoAlerta == filters.TipoAlerta.Value);
        }

        if (filters.ColaboradorId.HasValue)
        {
            query = query.Where(alerta => alerta.ColaboradorId == filters.ColaboradorId.Value);
        }

        if (filters.Desde.HasValue)
        {
            var desde = filters.Desde.Value.Date;
            query = query.Where(alerta => alerta.FechaVencimiento >= desde);
        }

        if (filters.Hasta.HasValue)
        {
            var hasta = filters.Hasta.Value.Date.AddDays(1).AddTicks(-1);
            query = query.Where(alerta => alerta.FechaVencimiento <= hasta);
        }

        var alertas = await query
            .OrderBy(alerta => alerta.FechaVencimiento)
            .ThenBy(alerta => alerta.TipoAlerta)
            .ToListAsync(cancellationToken);

        return ApiResponse<IReadOnlyCollection<AlertaListDto>>.Ok(
            alertas.Select(ToListDto).ToList());
    }

    public async Task<ApiResponse<AlertaResumenDto>> GetResumenAsync(
        CancellationToken cancellationToken = default)
    {
        var today = DateTime.UtcNow.Date;

        var alertas = await dbContext.Alertas
            .AsNoTracking()
            .Where(alerta => alerta.IsActive)
            .ToListAsync(cancellationToken);

        var resumen = new AlertaResumenDto
        {
            TotalAlertas = alertas.Count,
            Pendientes = alertas.Count(alerta => alerta.EstadoAlerta == EstadoAlerta.Pendiente),
            Vencidas = alertas.Count(alerta => alerta.EstadoAlerta == EstadoAlerta.Vencida),
            Gestionadas = alertas.Count(alerta => alerta.EstadoAlerta == EstadoAlerta.Gestionada),
            Ignoradas = alertas.Count(alerta => alerta.EstadoAlerta == EstadoAlerta.Ignorada),
            PorTipoAlerta = alertas
                .GroupBy(alerta => alerta.TipoAlerta)
                .OrderBy(group => group.Key)
                .Select(group => new AlertaPorTipoDto
                {
                    TipoAlerta = group.Key.ToString(),
                    Total = group.Count()
                })
                .ToList(),
            ProximasAVencer = alertas.Count(alerta =>
                alerta.EstadoAlerta == EstadoAlerta.Pendiente
                && alerta.FechaVencimiento.Date >= today
                && alerta.FechaVencimiento.Date <= today.AddDays(DiasAnticipacion)),
            VencidasPendientes = alertas.Count(alerta =>
                alerta.EstadoAlerta == EstadoAlerta.Vencida
                && alerta.FechaVencimiento.Date < today)
        };

        return ApiResponse<AlertaResumenDto>.Ok(resumen);
    }

    public async Task<ApiResponse<AlertaListDto>> GestionarAsync(
        int id,
        GestionarAlertaRequest request,
        ClaimsPrincipal principal,
        CancellationToken cancellationToken = default)
    {
        return await CambiarEstadoGestionAsync(
            id,
            EstadoAlerta.Gestionada,
            request,
            principal,
            "Alerta gestionada correctamente.",
            cancellationToken);
    }

    public async Task<ApiResponse<AlertaListDto>> IgnorarAsync(
        int id,
        GestionarAlertaRequest request,
        ClaimsPrincipal principal,
        CancellationToken cancellationToken = default)
    {
        return await CambiarEstadoGestionAsync(
            id,
            EstadoAlerta.Ignorada,
            request,
            principal,
            "Alerta ignorada correctamente.",
            cancellationToken);
    }

    public async Task<ApiResponse<RecalcularAlertasResultDto>> RecalcularAsync(
        ClaimsPrincipal principal,
        CancellationToken cancellationToken = default)
    {
        var currentUser = GetCurrentUser(principal);

        if (currentUser.UserId is null)
        {
            return ApiResponse<RecalcularAlertasResultDto>.Fail("Usuario autenticado invalido.");
        }

        var today = DateTime.UtcNow.Date;
        var limitDate = today.AddDays(DiasAnticipacion);
        var now = DateTime.UtcNow;

        await DesactivarAlertasNoOperativasAsync(
            currentUser.UserName,
            cancellationToken);

        var updatedToExpired = await ActualizarPendientesVencidasAsync(
            today,
            currentUser.UserName,
            cancellationToken);

        var existingKeys = await dbContext.Alertas
            .AsNoTracking()
            .Select(alerta => new AlertKey(
                alerta.TipoAlerta,
                alerta.ColaboradorId,
                alerta.DocumentoColaboradorId,
                alerta.FechaVencimiento.Date))
            .ToListAsync(cancellationToken);

        var existingKeySet = existingKeys.ToHashSet();
        var alertasNuevas = new List<Alerta>();

        await AgregarAlertasColaboradoresAsync(
            alertasNuevas,
            existingKeySet,
            today,
            limitDate,
            now,
            currentUser.UserName,
            cancellationToken);

        await AgregarAlertasDocumentosAsync(
            alertasNuevas,
            existingKeySet,
            today,
            limitDate,
            now,
            currentUser.UserName,
            cancellationToken);

        if (alertasNuevas.Count > 0)
        {
            dbContext.Alertas.AddRange(alertasNuevas);
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        var totalActivas = await dbContext.Alertas.CountAsync(
            alerta => alerta.IsActive,
            cancellationToken);

        return ApiResponse<RecalcularAlertasResultDto>.Ok(
            new RecalcularAlertasResultDto
            {
                AlertasCreadas = alertasNuevas.Count,
                AlertasActualizadasAVencidas = updatedToExpired,
                TotalAlertasActivas = totalActivas
            },
            "Alertas recalculadas correctamente.");
    }

    private IQueryable<Alerta> BaseQuery()
    {
        return dbContext.Alertas
            .Include(alerta => alerta.Colaborador)
            .Include(alerta => alerta.DocumentoColaborador)
                .ThenInclude(documento => documento!.TipoDocumento)
            .Include(alerta => alerta.UsuarioGestion);
    }

    private async Task<ApiResponse<AlertaListDto>> CambiarEstadoGestionAsync(
        int id,
        EstadoAlerta estado,
        GestionarAlertaRequest request,
        ClaimsPrincipal principal,
        string message,
        CancellationToken cancellationToken)
    {
        var currentUser = GetCurrentUser(principal);

        if (currentUser.UserId is null)
        {
            return ApiResponse<AlertaListDto>.Fail("Usuario autenticado invalido.");
        }

        var alerta = await dbContext.Alertas
            .SingleOrDefaultAsync(item => item.AlertaId == id, cancellationToken);

        if (alerta is null)
        {
            return ApiResponse<AlertaListDto>.Fail("Alerta no encontrada.");
        }

        alerta.EstadoAlerta = estado;
        alerta.FechaGestion = DateTime.UtcNow;
        alerta.GestionadaPor = currentUser.UserId.Value;
        alerta.ObservacionGestion = NormalizeNullable(request.ObservacionGestion);
        alerta.UpdatedAt = DateTime.UtcNow;
        alerta.UpdatedBy = currentUser.UserName;

        await dbContext.SaveChangesAsync(cancellationToken);

        var updated = await BaseQuery()
            .AsNoTracking()
            .SingleAsync(item => item.AlertaId == id, cancellationToken);

        return ApiResponse<AlertaListDto>.Ok(ToListDto(updated), message);
    }

    private async Task<int> ActualizarPendientesVencidasAsync(
        DateTime today,
        string? currentUser,
        CancellationToken cancellationToken)
    {
        var pendientesVencidas = await dbContext.Alertas
            .Where(alerta =>
                alerta.IsActive
                && EstatusOperativos.Contains(alerta.Colaborador.Estatus.Codigo)
                && alerta.EstadoAlerta == EstadoAlerta.Pendiente
                && alerta.FechaVencimiento < today)
            .ToListAsync(cancellationToken);

        foreach (var alerta in pendientesVencidas)
        {
            alerta.EstadoAlerta = EstadoAlerta.Vencida;
            alerta.UpdatedAt = DateTime.UtcNow;
            alerta.UpdatedBy = currentUser;
        }

        if (pendientesVencidas.Count > 0)
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        return pendientesVencidas.Count;
    }

    private async Task DesactivarAlertasNoOperativasAsync(
        string? currentUser,
        CancellationToken cancellationToken)
    {
        var alertasNoOperativas = await dbContext.Alertas
            .Where(alerta =>
                alerta.IsActive
                && !EstatusOperativos.Contains(alerta.Colaborador.Estatus.Codigo))
            .ToListAsync(cancellationToken);

        foreach (var alerta in alertasNoOperativas)
        {
            alerta.IsActive = false;
            alerta.UpdatedAt = DateTime.UtcNow;
            alerta.UpdatedBy = currentUser;
        }

        if (alertasNoOperativas.Count > 0)
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
    }

    private async Task AgregarAlertasColaboradoresAsync(
        List<Alerta> alertasNuevas,
        HashSet<AlertKey> existingKeys,
        DateTime today,
        DateTime limitDate,
        DateTime now,
        string? currentUser,
        CancellationToken cancellationToken)
    {
        var colaboradores = await dbContext.Colaboradores
            .AsNoTracking()
            .Where(colaborador =>
                colaborador.IsActive
                && EstatusOperativos.Contains(colaborador.Estatus.Codigo))
            .ToListAsync(cancellationToken);

        foreach (var colaborador in colaboradores)
        {
            var nombreCompleto = GetNombreCompleto(colaborador);

            TryAddColaboradorAlert(
                alertasNuevas,
                existingKeys,
                colaborador,
                TipoAlerta.Cedula,
                colaborador.FechaVencimientoCedula,
                $"La cedula de {nombreCompleto} vence el {{0:yyyy-MM-dd}}.",
                today,
                limitDate,
                now,
                currentUser);

            if (colaborador.TieneLicencia)
            {
                TryAddColaboradorAlert(
                    alertasNuevas,
                    existingKeys,
                    colaborador,
                    TipoAlerta.Licencia,
                    colaborador.FechaVencimientoLicencia,
                    $"La licencia de {nombreCompleto} vence el {{0:yyyy-MM-dd}}.",
                    today,
                    limitDate,
                    now,
                    currentUser);
            }

            TryAddColaboradorAlert(
                alertasNuevas,
                existingKeys,
                colaborador,
                TipoAlerta.Contrato,
                colaborador.FechaVencimientoContrato,
                $"El contrato de {nombreCompleto} vence el {{0:yyyy-MM-dd}}.",
                today,
                limitDate,
                now,
                currentUser);

            TryAddColaboradorAlert(
                alertasNuevas,
                existingKeys,
                colaborador,
                TipoAlerta.PeriodoProbatorio,
                colaborador.FechaVencimientoPeriodoProbatorio,
                $"El periodo probatorio de {nombreCompleto} vence el {{0:yyyy-MM-dd}}.",
                today,
                limitDate,
                now,
                currentUser);
        }
    }

    private async Task AgregarAlertasDocumentosAsync(
        List<Alerta> alertasNuevas,
        HashSet<AlertKey> existingKeys,
        DateTime today,
        DateTime limitDate,
        DateTime now,
        string? currentUser,
        CancellationToken cancellationToken)
    {
        var documentos = await dbContext.DocumentosColaborador
            .Include(documento => documento.Colaborador)
            .Include(documento => documento.TipoDocumento)
            .AsNoTracking()
            .Where(documento =>
                documento.IsActive
                && documento.Colaborador.IsActive
                && EstatusOperativos.Contains(documento.Colaborador.Estatus.Codigo)
                && documento.TieneVencimiento
                && documento.FechaVencimiento.HasValue)
            .ToListAsync(cancellationToken);

        foreach (var documento in documentos)
        {
            var fechaVencimiento = documento.FechaVencimiento!.Value.Date;

            if (!ShouldGenerate(fechaVencimiento, limitDate))
            {
                continue;
            }

            var key = new AlertKey(
                TipoAlerta.Documento,
                documento.ColaboradorId,
                documento.DocumentoColaboradorId,
                fechaVencimiento);

            if (!existingKeys.Add(key))
            {
                continue;
            }

            var nombreCompleto = GetNombreCompleto(documento.Colaborador);

            alertasNuevas.Add(CreateAlert(
                TipoAlerta.Documento,
                documento.ColaboradorId,
                documento.DocumentoColaboradorId,
                fechaVencimiento,
                string.Format(
                    "El documento {0} de {1} vence el {2:yyyy-MM-dd}.",
                    documento.TipoDocumento.Nombre,
                    nombreCompleto,
                    fechaVencimiento),
                today,
                now,
                currentUser));
        }
    }

    private static void TryAddColaboradorAlert(
        List<Alerta> alertasNuevas,
        HashSet<AlertKey> existingKeys,
        Colaborador colaborador,
        TipoAlerta tipoAlerta,
        DateTime? fecha,
        string messageFormat,
        DateTime today,
        DateTime limitDate,
        DateTime now,
        string? currentUser)
    {
        if (!fecha.HasValue)
        {
            return;
        }

        var fechaVencimiento = fecha.Value.Date;

        if (!ShouldGenerate(fechaVencimiento, limitDate))
        {
            return;
        }

        var key = new AlertKey(tipoAlerta, colaborador.ColaboradorId, null, fechaVencimiento);

        if (!existingKeys.Add(key))
        {
            return;
        }

        alertasNuevas.Add(CreateAlert(
            tipoAlerta,
            colaborador.ColaboradorId,
            null,
            fechaVencimiento,
            string.Format(messageFormat, fechaVencimiento),
            today,
            now,
            currentUser));
    }

    private static Alerta CreateAlert(
        TipoAlerta tipoAlerta,
        int colaboradorId,
        int? documentoColaboradorId,
        DateTime fechaVencimiento,
        string mensaje,
        DateTime today,
        DateTime now,
        string? currentUser)
    {
        return new Alerta
        {
            TipoAlerta = tipoAlerta,
            EstadoAlerta = fechaVencimiento < today ? EstadoAlerta.Vencida : EstadoAlerta.Pendiente,
            ColaboradorId = colaboradorId,
            DocumentoColaboradorId = documentoColaboradorId,
            FechaVencimiento = fechaVencimiento,
            Mensaje = mensaje,
            FechaGeneracion = now,
            CreatedAt = now,
            CreatedBy = currentUser,
            UpdatedAt = now,
            UpdatedBy = currentUser,
            IsActive = true
        };
    }

    private static bool ShouldGenerate(DateTime fechaVencimiento, DateTime limitDate)
    {
        return fechaVencimiento <= limitDate;
    }

    private static AlertaListDto ToListDto(Alerta alerta)
    {
        return new AlertaListDto
        {
            AlertaId = alerta.AlertaId,
            TipoAlerta = alerta.TipoAlerta.ToString(),
            EstadoAlerta = alerta.EstadoAlerta.ToString(),
            ColaboradorId = alerta.ColaboradorId,
            NombreCompletoColaborador = GetNombreCompleto(alerta.Colaborador),
            DocumentoColaboradorId = alerta.DocumentoColaboradorId,
            TipoDocumentoNombre = alerta.DocumentoColaborador?.TipoDocumento.Nombre,
            FechaVencimiento = alerta.FechaVencimiento,
            Mensaje = alerta.Mensaje,
            FechaGeneracion = alerta.FechaGeneracion,
            FechaGestion = alerta.FechaGestion,
            GestionadaPor = alerta.GestionadaPor,
            GestionadaPorNombre = alerta.UsuarioGestion?.NombreUsuario,
            ObservacionGestion = alerta.ObservacionGestion,
            IsActive = alerta.IsActive
        };
    }

    private static CurrentUser GetCurrentUser(ClaimsPrincipal principal)
    {
        var userIdValue = principal.FindFirstValue("UserId")
            ?? principal.FindFirstValue(ClaimTypes.NameIdentifier);

        return new CurrentUser(
            int.TryParse(userIdValue, out var userId) ? userId : null,
            principal.Identity?.Name);
    }

    private static string GetNombreCompleto(Colaborador colaborador)
    {
        return string.Join(
            " ",
            new[]
            {
                colaborador.PrimerNombre,
                colaborador.SegundoNombre,
                colaborador.PrimerApellido,
                colaborador.SegundoApellido
            }.Where(value => !string.IsNullOrWhiteSpace(value)));
    }

    private static string? NormalizeNullable(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    private sealed record CurrentUser(int? UserId, string? UserName);

    private sealed record AlertKey(
        TipoAlerta TipoAlerta,
        int ColaboradorId,
        int? DocumentoColaboradorId,
        DateTime FechaVencimiento);
}
