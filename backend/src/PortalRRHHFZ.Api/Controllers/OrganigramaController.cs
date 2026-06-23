using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PortalRRHHFZ.Application.Common;
using PortalRRHHFZ.Application.DTOs;
using PortalRRHHFZ.Domain.Constants;
using PortalRRHHFZ.Domain.Entities;
using PortalRRHHFZ.Infrastructure.Data;

namespace PortalRRHHFZ.Api.Controllers;

[ApiController]
[Authorize(Policy = AppPolicies.RequireSolicitudes)]
[Route("api/organigrama")]
public sealed class OrganigramaController(AppDbContext db) : ControllerBase
{
    private static readonly string[] OperationalStatusCodes = ["A", "V", "S"];
    private static readonly HashSet<string> ValidResponsibleTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "LiderPrincipal",
        "LiderAlterno",
        "Supervisor",
        "Coordinador",
        "Gerente",
        "RRHHApoyo",
        "Otro"
    };

    [HttpGet]
    public async Task<IActionResult> Get([FromQuery] int? empresaId, [FromQuery] bool soloActivos = true, CancellationToken cancellationToken = default)
    {
        if (!CanReadOrganigrama())
        {
            return Forbid();
        }

        var query = db.Organigramas
            .Include(x => x.Empresa)
            .Include(x => x.Nodos)
            .AsNoTracking()
            .AsQueryable();

        if (soloActivos)
        {
            query = query.Where(x => x.IsActive);
        }

        if (empresaId.HasValue)
        {
            query = query.Where(x => x.EmpresaId == empresaId.Value);
        }

        var data = await query
            .OrderByDescending(x => x.IsActive)
            .ThenBy(x => x.Nombre)
            .Select(x => new OrganigramaListDto(
                x.OrganigramaId,
                x.Nombre,
                x.EmpresaId,
                x.Empresa != null ? x.Empresa.Nombre : null,
                x.Descripcion,
                x.FechaInicio,
                x.FechaFin,
                x.Nodos.Count,
                x.IsActive))
            .ToListAsync(cancellationToken);

        return Ok(ApiResponse<List<OrganigramaListDto>>.Ok(data));
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id, CancellationToken cancellationToken)
    {
        if (!CanReadOrganigrama())
        {
            return Forbid();
        }

        var organigrama = await db.Organigramas
            .Include(x => x.Empresa)
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.OrganigramaId == id, cancellationToken);

        if (organigrama is null)
        {
            return NotFound(ApiResponse<object>.Fail("Organigrama no encontrado."));
        }

        var nodes = await LoadNodeDtosAsync(id, cancellationToken);
        var history = await db.OrganigramaHistorialCambios
            .Include(x => x.Usuario)
            .AsNoTracking()
            .Where(x => x.Entidad == nameof(Organigrama) && x.EntidadId == id)
            .OrderByDescending(x => x.Fecha)
            .Take(30)
            .Select(x => new OrganigramaHistorialCambioDto(
                x.OrganigramaHistorialCambioId,
                x.Entidad,
                x.EntidadId,
                x.Accion,
                x.ValorAnterior,
                x.ValorNuevo,
                x.UsuarioId,
                x.Usuario.NombreUsuario,
                x.Fecha,
                x.Comentario))
            .ToListAsync(cancellationToken);

        return Ok(ApiResponse<OrganigramaDetailDto>.Ok(new OrganigramaDetailDto(
            organigrama.OrganigramaId,
            organigrama.Nombre,
            organigrama.EmpresaId,
            organigrama.Empresa?.Nombre,
            organigrama.Descripcion,
            organigrama.FechaInicio,
            organigrama.FechaFin,
            organigrama.IsActive,
            nodes,
            history)));
    }

    [HttpPost]
    [Authorize(Policy = AppPolicies.RequireAdmin)]
    public async Task<IActionResult> Create([FromBody] CreateOrganigramaRequest request, CancellationToken cancellationToken)
    {
        var validation = await ValidateOrganigramaAsync(request, cancellationToken);
        if (validation is not null)
        {
            return BadRequest(ApiResponse<object>.Fail(validation));
        }

        var organigrama = new Organigrama
        {
            Nombre = request.Nombre.Trim(),
            EmpresaId = request.EmpresaId,
            Descripcion = request.Descripcion?.Trim(),
            FechaInicio = request.FechaInicio.Date,
            FechaFin = request.FechaFin?.Date,
            IsActive = request.IsActive,
            CreatedBy = CurrentUserName()
        };

        db.Organigramas.Add(organigrama);
        await db.SaveChangesAsync(cancellationToken);
        AddHistory(nameof(Organigrama), organigrama.OrganigramaId, "CREACION", null, ToJson(organigrama), "Creacion de organigrama funcional.");
        await db.SaveChangesAsync(cancellationToken);

        return CreatedAtAction(nameof(GetById), new { id = organigrama.OrganigramaId }, ApiResponse<object>.Ok(new { organigrama.OrganigramaId }, "Organigrama creado."));
    }

    [HttpPut("{id:int}")]
    [HttpPatch("{id:int}")]
    [Authorize(Policy = AppPolicies.RequireAdmin)]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateOrganigramaRequest request, CancellationToken cancellationToken)
    {
        var organigrama = await db.Organigramas.FirstOrDefaultAsync(x => x.OrganigramaId == id, cancellationToken);
        if (organigrama is null)
        {
            return NotFound(ApiResponse<object>.Fail("Organigrama no encontrado."));
        }

        var validation = await ValidateOrganigramaAsync(request, cancellationToken);
        if (validation is not null)
        {
            return BadRequest(ApiResponse<object>.Fail(validation));
        }

        var before = ToJson(organigrama);
        organigrama.Nombre = request.Nombre.Trim();
        organigrama.EmpresaId = request.EmpresaId;
        organigrama.Descripcion = request.Descripcion?.Trim();
        organigrama.FechaInicio = request.FechaInicio.Date;
        organigrama.FechaFin = request.FechaFin?.Date;
        organigrama.IsActive = request.IsActive;
        organigrama.UpdatedBy = CurrentUserName();
        AddHistory(nameof(Organigrama), organigrama.OrganigramaId, "ACTUALIZACION", before, ToJson(organigrama), "Actualizacion de organigrama funcional.");

        await db.SaveChangesAsync(cancellationToken);
        return Ok(ApiResponse<object>.Ok(new { organigrama.OrganigramaId }, "Organigrama actualizado."));
    }

    [HttpPatch("{id:int}/desactivar")]
    [Authorize(Policy = AppPolicies.RequireAdmin)]
    public Task<IActionResult> Desactivar(int id, CancellationToken cancellationToken) => ToggleOrganigrama(id, false, cancellationToken);

    [HttpPatch("{id:int}/activar")]
    [Authorize(Policy = AppPolicies.RequireAdmin)]
    public Task<IActionResult> Activar(int id, CancellationToken cancellationToken) => ToggleOrganigrama(id, true, cancellationToken);

    [HttpGet("{id:int}/nodos")]
    public async Task<IActionResult> GetNodes(int id, CancellationToken cancellationToken)
    {
        if (!CanReadOrganigrama())
        {
            return Forbid();
        }

        if (!await db.Organigramas.AnyAsync(x => x.OrganigramaId == id, cancellationToken))
        {
            return NotFound(ApiResponse<object>.Fail("Organigrama no encontrado."));
        }

        return Ok(ApiResponse<List<OrganigramaNodoDto>>.Ok(await LoadNodeDtosAsync(id, cancellationToken)));
    }

    [HttpPost("{id:int}/nodos")]
    [Authorize(Policy = AppPolicies.RequireAdmin)]
    public async Task<IActionResult> CreateNode(int id, [FromBody] CreateOrganigramaNodoRequest request, CancellationToken cancellationToken)
    {
        if (!await db.Organigramas.AnyAsync(x => x.OrganigramaId == id, cancellationToken))
        {
            return NotFound(ApiResponse<object>.Fail("Organigrama no encontrado."));
        }

        var validation = await ValidateNodeAsync(id, null, request, cancellationToken);
        if (validation is not null)
        {
            return BadRequest(ApiResponse<object>.Fail(validation));
        }

        var parent = request.NodoPadreId.HasValue
            ? await db.OrganigramaNodos.AsNoTracking().FirstAsync(x => x.OrganigramaNodoId == request.NodoPadreId.Value, cancellationToken)
            : null;

        var node = new OrganigramaNodo
        {
            OrganigramaId = id,
            EmpresaId = request.EmpresaId,
            DepartamentoId = request.DepartamentoId,
            CargoId = request.CargoId,
            NodoPadreId = request.NodoPadreId,
            NombreNodo = request.NombreNodo.Trim(),
            Descripcion = request.Descripcion?.Trim(),
            Nivel = parent is null ? Math.Max(0, request.Nivel) : parent.Nivel + 1,
            Orden = request.Orden,
            EsRolOperativo = request.EsRolOperativo,
            IsActive = request.IsActive,
            CreatedBy = CurrentUserName()
        };

        db.OrganigramaNodos.Add(node);
        await db.SaveChangesAsync(cancellationToken);
        AddHistory(nameof(OrganigramaNodo), node.OrganigramaNodoId, "CREACION", null, ToJson(node), "Creacion de nodo de organigrama.");
        await db.SaveChangesAsync(cancellationToken);

        return CreatedAtAction(nameof(GetNodes), new { id }, ApiResponse<object>.Ok(new { node.OrganigramaNodoId }, "Nodo creado."));
    }

    [HttpPut("nodos/{nodoId:int}")]
    [HttpPatch("nodos/{nodoId:int}")]
    [Authorize(Policy = AppPolicies.RequireAdmin)]
    public async Task<IActionResult> UpdateNode(int nodoId, [FromBody] UpdateOrganigramaNodoRequest request, CancellationToken cancellationToken)
    {
        var node = await db.OrganigramaNodos.FirstOrDefaultAsync(x => x.OrganigramaNodoId == nodoId, cancellationToken);
        if (node is null)
        {
            return NotFound(ApiResponse<object>.Fail("Nodo no encontrado."));
        }

        var validation = await ValidateNodeAsync(node.OrganigramaId, nodoId, request, cancellationToken);
        if (validation is not null)
        {
            return BadRequest(ApiResponse<object>.Fail(validation));
        }

        var parent = request.NodoPadreId.HasValue
            ? await db.OrganigramaNodos.AsNoTracking().FirstAsync(x => x.OrganigramaNodoId == request.NodoPadreId.Value, cancellationToken)
            : null;

        var before = ToJson(node);
        node.EmpresaId = request.EmpresaId;
        node.DepartamentoId = request.DepartamentoId;
        node.CargoId = request.CargoId;
        node.NodoPadreId = request.NodoPadreId;
        node.NombreNodo = request.NombreNodo.Trim();
        node.Descripcion = request.Descripcion?.Trim();
        node.Nivel = parent is null ? Math.Max(0, request.Nivel) : parent.Nivel + 1;
        node.Orden = request.Orden;
        node.EsRolOperativo = request.EsRolOperativo;
        node.IsActive = request.IsActive;
        node.UpdatedBy = CurrentUserName();
        AddHistory(nameof(OrganigramaNodo), node.OrganigramaNodoId, "ACTUALIZACION", before, ToJson(node), "Actualizacion de nodo de organigrama.");

        await db.SaveChangesAsync(cancellationToken);
        return Ok(ApiResponse<object>.Ok(new { node.OrganigramaNodoId }, "Nodo actualizado."));
    }

    [HttpPatch("nodos/{nodoId:int}/desactivar")]
    [Authorize(Policy = AppPolicies.RequireAdmin)]
    public Task<IActionResult> DesactivarNode(int nodoId, CancellationToken cancellationToken) => ToggleNode(nodoId, false, cancellationToken);

    [HttpPatch("nodos/{nodoId:int}/activar")]
    [Authorize(Policy = AppPolicies.RequireAdmin)]
    public Task<IActionResult> ActivarNode(int nodoId, CancellationToken cancellationToken) => ToggleNode(nodoId, true, cancellationToken);

    [HttpGet("responsables")]
    public async Task<IActionResult> GetResponsables([FromQuery] int? empresaId, [FromQuery] int? departamentoId, [FromQuery] bool soloActivos = true, CancellationToken cancellationToken = default)
    {
        if (!CanReadOrganigrama())
        {
            return Forbid();
        }

        var data = await LoadResponsablesAsync(empresaId, departamentoId, soloActivos, cancellationToken);
        return Ok(ApiResponse<List<DepartamentoResponsableDto>>.Ok(data));
    }

    [HttpPost("responsables")]
    [Authorize(Policy = AppPolicies.RequireAdmin)]
    public async Task<IActionResult> CreateResponsable([FromBody] CreateDepartamentoResponsableRequest request, CancellationToken cancellationToken)
    {
        var validation = await ValidateResponsableAsync(null, request, cancellationToken);
        if (validation is not null)
        {
            return BadRequest(ApiResponse<object>.Fail(validation));
        }

        var responsable = new DepartamentoResponsable
        {
            EmpresaId = request.EmpresaId,
            DepartamentoId = request.DepartamentoId,
            ColaboradorResponsableId = request.ColaboradorResponsableId,
            UsuarioResponsableId = request.UsuarioResponsableId,
            TipoResponsable = NormalizeResponsibleType(request.TipoResponsable),
            EsPrincipal = request.EsPrincipal,
            PuedeAprobarSolicitudes = request.PuedeAprobarSolicitudes,
            FechaInicio = request.FechaInicio.Date,
            FechaFin = request.FechaFin?.Date,
            Observacion = request.Observacion?.Trim(),
            IsActive = request.IsActive,
            CreatedBy = CurrentUserName()
        };

        db.DepartamentoResponsables.Add(responsable);
        await db.SaveChangesAsync(cancellationToken);
        AddHistory(nameof(DepartamentoResponsable), responsable.DepartamentoResponsableId, "CREACION", null, ToJson(responsable), "Creacion de responsable por departamento.");
        await db.SaveChangesAsync(cancellationToken);

        return CreatedAtAction(nameof(GetResponsables), ApiResponse<object>.Ok(new { responsable.DepartamentoResponsableId }, "Responsable creado."));
    }

    [HttpPut("responsables/{id:int}")]
    [HttpPatch("responsables/{id:int}")]
    [Authorize(Policy = AppPolicies.RequireAdmin)]
    public async Task<IActionResult> UpdateResponsable(int id, [FromBody] UpdateDepartamentoResponsableRequest request, CancellationToken cancellationToken)
    {
        var responsable = await db.DepartamentoResponsables.FirstOrDefaultAsync(x => x.DepartamentoResponsableId == id, cancellationToken);
        if (responsable is null)
        {
            return NotFound(ApiResponse<object>.Fail("Responsable no encontrado."));
        }

        var validation = await ValidateResponsableAsync(id, request, cancellationToken);
        if (validation is not null)
        {
            return BadRequest(ApiResponse<object>.Fail(validation));
        }

        var before = ToJson(responsable);
        responsable.EmpresaId = request.EmpresaId;
        responsable.DepartamentoId = request.DepartamentoId;
        responsable.ColaboradorResponsableId = request.ColaboradorResponsableId;
        responsable.UsuarioResponsableId = request.UsuarioResponsableId;
        responsable.TipoResponsable = NormalizeResponsibleType(request.TipoResponsable);
        responsable.EsPrincipal = request.EsPrincipal;
        responsable.PuedeAprobarSolicitudes = request.PuedeAprobarSolicitudes;
        responsable.FechaInicio = request.FechaInicio.Date;
        responsable.FechaFin = request.FechaFin?.Date;
        responsable.Observacion = request.Observacion?.Trim();
        responsable.IsActive = request.IsActive;
        responsable.UpdatedBy = CurrentUserName();
        AddHistory(nameof(DepartamentoResponsable), responsable.DepartamentoResponsableId, "ACTUALIZACION", before, ToJson(responsable), "Actualizacion de responsable por departamento.");

        await db.SaveChangesAsync(cancellationToken);
        return Ok(ApiResponse<object>.Ok(new { responsable.DepartamentoResponsableId }, "Responsable actualizado."));
    }

    [HttpPatch("responsables/{id:int}/desactivar")]
    [Authorize(Policy = AppPolicies.RequireAdmin)]
    public Task<IActionResult> DesactivarResponsable(int id, CancellationToken cancellationToken) => ToggleResponsable(id, false, cancellationToken);

    [HttpPatch("responsables/{id:int}/activar")]
    [Authorize(Policy = AppPolicies.RequireAdmin)]
    public Task<IActionResult> ActivarResponsable(int id, CancellationToken cancellationToken) => ToggleResponsable(id, true, cancellationToken);

    [HttpGet("aprobadores")]
    public async Task<IActionResult> Aprobadores(
        [FromQuery] int? empresaId,
        [FromQuery] int? departamentoId,
        [FromQuery] bool puedeAprobarSolicitudes = true,
        CancellationToken cancellationToken = default)
    {
        var query = db.DepartamentoResponsables
            .Include(x => x.Empresa)
            .Include(x => x.Departamento)
            .Include(x => x.ColaboradorResponsable).ThenInclude(x => x.Empresa)
            .Include(x => x.ColaboradorResponsable).ThenInclude(x => x.Departamento)
            .Include(x => x.ColaboradorResponsable).ThenInclude(x => x.Cargo)
            .Include(x => x.ColaboradorResponsable).ThenInclude(x => x.Estatus)
            .Include(x => x.UsuarioResponsable)
            .AsNoTracking()
            .Where(x => x.IsActive && (!x.FechaFin.HasValue || x.FechaFin.Value.Date >= DateTime.Today));

        if (puedeAprobarSolicitudes)
        {
            query = query.Where(x => x.PuedeAprobarSolicitudes);
        }

        if (empresaId.HasValue)
        {
            query = query.Where(x => x.EmpresaId == empresaId.Value);
        }

        if (departamentoId.HasValue)
        {
            query = query.Where(x => x.DepartamentoId == departamentoId.Value);
        }

        var responsables = await query
            .OrderByDescending(x => x.EsPrincipal)
            .ThenBy(x => x.TipoResponsable)
            .ThenBy(x => x.ColaboradorResponsable.PrimerApellido)
            .ToListAsync(cancellationToken);

        var data = responsables
            .Where(x => IsOperationalCollaborator(x.ColaboradorResponsable))
            .Select(ToAprobadorDto)
            .ToList();

        return Ok(ApiResponse<List<AprobadorSolicitudDto>>.Ok(data));
    }

    [HttpGet("colaboradores-activos")]
    public async Task<IActionResult> GetColaboradoresActivos(
        [FromQuery] string? search,
        [FromQuery] int? empresaId,
        [FromQuery] int? departamentoId,
        [FromQuery] int take = 25,
        CancellationToken cancellationToken = default)
    {
        if (!CanReadOrganigrama())
        {
            return Forbid();
        }

        var colaboradores = await ActiveCollaboratorsQuery(search, empresaId, departamentoId)
            .OrderBy(x => x.PrimerApellido)
            .ThenBy(x => x.PrimerNombre)
            .Take(Math.Clamp(take, 1, 50))
            .ToListAsync(cancellationToken);

        var data = colaboradores
            .Select(x => new ColaboradorLookupDto(
                x.ColaboradorId,
                x.NoEmpleado,
                x.NombreCompleto(),
                x.EmpresaId,
                x.Empresa.Nombre,
                x.DepartamentoId,
                x.Departamento.Nombre,
                x.CargoId,
                x.Cargo.Nombre,
                x.Estatus.Nombre))
            .ToList();

        return Ok(ApiResponse<List<ColaboradorLookupDto>>.Ok(data));
    }

    [HttpGet("nodos/{id:int}/colaboradores-activos")]
    public async Task<IActionResult> GetNodeCollaborators(int id, CancellationToken cancellationToken)
    {
        if (!CanReadOrganigrama())
        {
            return Forbid();
        }

        var node = await db.OrganigramaNodos.AsNoTracking().FirstOrDefaultAsync(x => x.OrganigramaNodoId == id, cancellationToken);
        if (node is null)
        {
            return NotFound(ApiResponse<object>.Fail("Nodo no encontrado."));
        }

        var colaboradores = await NodeCollaboratorsQuery(node)
            .OrderBy(x => x.PrimerApellido)
            .ThenBy(x => x.PrimerNombre)
            .ToListAsync(cancellationToken);

        var data = colaboradores
            .Select(x => new ColaboradorLookupDto(
                x.ColaboradorId,
                x.NoEmpleado,
                x.NombreCompleto(),
                x.EmpresaId,
                x.Empresa.Nombre,
                x.DepartamentoId,
                x.Departamento.Nombre,
                x.CargoId,
                x.Cargo.Nombre,
                x.Estatus.Nombre))
            .ToList();

        return Ok(ApiResponse<List<ColaboradorLookupDto>>.Ok(data));
    }

    private async Task<IActionResult> ToggleOrganigrama(int id, bool active, CancellationToken cancellationToken)
    {
        var organigrama = await db.Organigramas.FirstOrDefaultAsync(x => x.OrganigramaId == id, cancellationToken);
        if (organigrama is null)
        {
            return NotFound(ApiResponse<object>.Fail("Organigrama no encontrado."));
        }

        var before = ToJson(organigrama);
        organigrama.IsActive = active;
        organigrama.UpdatedBy = CurrentUserName();
        AddHistory(nameof(Organigrama), organigrama.OrganigramaId, active ? "ACTIVACION" : "DESACTIVACION", before, ToJson(organigrama), active ? "Organigrama activado." : "Organigrama desactivado.");
        await db.SaveChangesAsync(cancellationToken);
        return Ok(ApiResponse<object>.Ok(new { organigrama.OrganigramaId }, active ? "Organigrama activado." : "Organigrama desactivado."));
    }

    private async Task<IActionResult> ToggleNode(int id, bool active, CancellationToken cancellationToken)
    {
        var node = await db.OrganigramaNodos.FirstOrDefaultAsync(x => x.OrganigramaNodoId == id, cancellationToken);
        if (node is null)
        {
            return NotFound(ApiResponse<object>.Fail("Nodo no encontrado."));
        }

        var before = ToJson(node);
        node.IsActive = active;
        node.UpdatedBy = CurrentUserName();
        AddHistory(nameof(OrganigramaNodo), node.OrganigramaNodoId, active ? "ACTIVACION" : "DESACTIVACION", before, ToJson(node), active ? "Nodo activado." : "Nodo desactivado.");
        await db.SaveChangesAsync(cancellationToken);
        return Ok(ApiResponse<object>.Ok(new { node.OrganigramaNodoId }, active ? "Nodo activado." : "Nodo desactivado."));
    }

    private async Task<IActionResult> ToggleResponsable(int id, bool active, CancellationToken cancellationToken)
    {
        var responsable = await db.DepartamentoResponsables.FirstOrDefaultAsync(x => x.DepartamentoResponsableId == id, cancellationToken);
        if (responsable is null)
        {
            return NotFound(ApiResponse<object>.Fail("Responsable no encontrado."));
        }

        var before = ToJson(responsable);
        responsable.IsActive = active;
        responsable.UpdatedBy = CurrentUserName();
        AddHistory(nameof(DepartamentoResponsable), responsable.DepartamentoResponsableId, active ? "ACTIVACION" : "DESACTIVACION", before, ToJson(responsable), active ? "Responsable activado." : "Responsable desactivado.");
        await db.SaveChangesAsync(cancellationToken);
        return Ok(ApiResponse<object>.Ok(new { responsable.DepartamentoResponsableId }, active ? "Responsable activado." : "Responsable desactivado."));
    }

    private async Task<string?> ValidateOrganigramaAsync(CreateOrganigramaRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Nombre))
        {
            return "Nombre de organigrama es obligatorio.";
        }

        if (request.FechaFin.HasValue && request.FechaFin.Value.Date < request.FechaInicio.Date)
        {
            return "Fecha fin no puede ser anterior a fecha inicio.";
        }

        if (request.EmpresaId.HasValue && !await db.Empresas.AnyAsync(x => x.EmpresaId == request.EmpresaId.Value && x.IsActive, cancellationToken))
        {
            return "Empresa no valida.";
        }

        return null;
    }

    private async Task<string?> ValidateNodeAsync(int organigramaId, int? nodeId, CreateOrganigramaNodoRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.NombreNodo))
        {
            return "Nombre de nodo es obligatorio.";
        }

        if (request.EmpresaId.HasValue && !await db.Empresas.AnyAsync(x => x.EmpresaId == request.EmpresaId.Value && x.IsActive, cancellationToken))
        {
            return "Empresa de nodo no valida.";
        }

        if (request.DepartamentoId.HasValue)
        {
            var departamento = await db.Departamentos.AsNoTracking().FirstOrDefaultAsync(x => x.DepartamentoId == request.DepartamentoId.Value && x.IsActive, cancellationToken);
            if (departamento is null)
            {
                return "Departamento de nodo no valido.";
            }

            if (request.EmpresaId.HasValue && departamento.EmpresaId != request.EmpresaId.Value)
            {
                return "Departamento no pertenece a la empresa del nodo.";
            }
        }

        if (request.CargoId.HasValue)
        {
            var cargo = await db.Cargos.AsNoTracking().FirstOrDefaultAsync(x => x.CargoId == request.CargoId.Value && x.IsActive, cancellationToken);
            if (cargo is null)
            {
                return "Cargo de nodo no valido.";
            }

            if (request.DepartamentoId.HasValue && cargo.DepartamentoId != request.DepartamentoId.Value)
            {
                return "Cargo no pertenece al departamento del nodo.";
            }
        }

        if (request.NodoPadreId.HasValue)
        {
            var parent = await db.OrganigramaNodos.AsNoTracking().FirstOrDefaultAsync(x => x.OrganigramaNodoId == request.NodoPadreId.Value, cancellationToken);
            if (parent is null || parent.OrganigramaId != organigramaId)
            {
                return "Nodo padre no pertenece al mismo organigrama.";
            }

            if (nodeId.HasValue && parent.OrganigramaNodoId == nodeId.Value)
            {
                return "Un nodo no puede ser padre de si mismo.";
            }

            if (nodeId.HasValue && await WouldCreateCycleAsync(nodeId.Value, parent.OrganigramaNodoId, cancellationToken))
            {
                return "La relacion de nodo padre generaria un ciclo.";
            }
        }

        return null;
    }

    private async Task<string?> ValidateResponsableAsync(int? responsableId, CreateDepartamentoResponsableRequest request, CancellationToken cancellationToken)
    {
        if (!ValidResponsibleTypes.Contains(request.TipoResponsable))
        {
            return "Tipo de responsable no valido.";
        }

        var empresaExists = await db.Empresas.AnyAsync(x => x.EmpresaId == request.EmpresaId && x.IsActive, cancellationToken);
        if (!empresaExists)
        {
            return "Empresa no valida.";
        }

        var departamento = await db.Departamentos.AsNoTracking().FirstOrDefaultAsync(x => x.DepartamentoId == request.DepartamentoId && x.IsActive, cancellationToken);
        if (departamento is null || departamento.EmpresaId != request.EmpresaId)
        {
            return "Departamento no pertenece a la empresa seleccionada.";
        }

        var colaborador = await db.Colaboradores
            .Include(x => x.Estatus)
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.ColaboradorId == request.ColaboradorResponsableId, cancellationToken);
        if (colaborador is null)
        {
            return "Colaborador responsable no encontrado.";
        }

        if (!IsOperationalCollaborator(colaborador))
        {
            return "Colaborador responsable no esta activo para organigrama operativo.";
        }

        if (request.UsuarioResponsableId.HasValue)
        {
            var usuario = await db.Usuarios
                .Include(x => x.Rol)
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.UsuarioId == request.UsuarioResponsableId.Value, cancellationToken);

            if (usuario is null || !usuario.IsActive)
            {
                return "Usuario responsable no valido o inactivo.";
            }

            if (usuario.Rol.Nombre is not (AppRoles.Supervisor or AppRoles.RRHH or AppRoles.Admin))
            {
                return "Usuario responsable no tiene rol aprobador.";
            }
        }

        if (request.EsPrincipal)
        {
            var tipo = NormalizeResponsibleType(request.TipoResponsable);
            var principalExists = await db.DepartamentoResponsables.AnyAsync(x =>
                x.DepartamentoResponsableId != responsableId &&
                x.EmpresaId == request.EmpresaId &&
                x.DepartamentoId == request.DepartamentoId &&
                x.TipoResponsable == tipo &&
                x.EsPrincipal &&
                x.IsActive &&
                (!x.FechaFin.HasValue || x.FechaFin.Value.Date >= DateTime.Today),
                cancellationToken);

            if (principalExists)
            {
                return "Ya existe un responsable principal activo con ese tipo para el departamento.";
            }
        }

        if (request.FechaFin.HasValue && request.FechaFin.Value.Date < request.FechaInicio.Date)
        {
            return "Fecha fin no puede ser anterior a fecha inicio.";
        }

        return null;
    }

    private async Task<List<OrganigramaNodoDto>> LoadNodeDtosAsync(int organigramaId, CancellationToken cancellationToken)
    {
        var nodes = await db.OrganigramaNodos
            .Include(x => x.Empresa)
            .Include(x => x.Departamento)
            .Include(x => x.Cargo)
            .Include(x => x.NodoPadre)
            .AsNoTracking()
            .Where(x => x.OrganigramaId == organigramaId)
            .OrderBy(x => x.Nivel)
            .ThenBy(x => x.Orden)
            .ThenBy(x => x.NombreNodo)
            .ToListAsync(cancellationToken);

        var data = new List<OrganigramaNodoDto>();
        foreach (var node in nodes)
        {
            data.Add(new OrganigramaNodoDto(
                node.OrganigramaNodoId,
                node.OrganigramaId,
                node.NodoPadreId,
                node.NodoPadre?.NombreNodo,
                node.NombreNodo,
                node.EmpresaId,
                node.Empresa?.Nombre,
                node.DepartamentoId,
                node.Departamento?.Nombre,
                node.CargoId,
                node.Cargo?.Nombre,
                node.Nivel,
                node.Orden,
                node.EsRolOperativo,
                await NodeCollaboratorsQuery(node).CountAsync(cancellationToken),
                node.Descripcion,
                node.IsActive));
        }

        return data;
    }

    private async Task<List<DepartamentoResponsableDto>> LoadResponsablesAsync(int? empresaId, int? departamentoId, bool soloActivos, CancellationToken cancellationToken)
    {
        var query = db.DepartamentoResponsables
            .Include(x => x.Empresa)
            .Include(x => x.Departamento)
            .Include(x => x.ColaboradorResponsable).ThenInclude(x => x.Empresa)
            .Include(x => x.ColaboradorResponsable).ThenInclude(x => x.Departamento)
            .Include(x => x.ColaboradorResponsable).ThenInclude(x => x.Cargo)
            .Include(x => x.ColaboradorResponsable).ThenInclude(x => x.Estatus)
            .Include(x => x.UsuarioResponsable)
            .AsNoTracking()
            .AsQueryable();

        if (soloActivos)
        {
            query = query.Where(x => x.IsActive);
        }

        if (empresaId.HasValue)
        {
            query = query.Where(x => x.EmpresaId == empresaId.Value);
        }

        if (departamentoId.HasValue)
        {
            query = query.Where(x => x.DepartamentoId == departamentoId.Value);
        }

        var responsables = await query
            .OrderBy(x => x.Empresa.Nombre)
            .ThenBy(x => x.Departamento.Nombre)
            .ThenByDescending(x => x.EsPrincipal)
            .ThenBy(x => x.TipoResponsable)
            .ToListAsync(cancellationToken);

        return responsables.Select(ToResponsableDto).ToList();
    }

    private IQueryable<Colaborador> ActiveCollaboratorsQuery(string? search, int? empresaId, int? departamentoId)
    {
        var query = db.Colaboradores
            .Include(x => x.Empresa)
            .Include(x => x.Departamento)
            .Include(x => x.Cargo)
            .Include(x => x.Estatus)
            .AsNoTracking()
            .Where(x => x.IsActive && OperationalStatusCodes.Contains(x.Estatus.Codigo));

        if (empresaId.HasValue)
        {
            query = query.Where(x => x.EmpresaId == empresaId.Value);
        }

        if (departamentoId.HasValue)
        {
            query = query.Where(x => x.DepartamentoId == departamentoId.Value);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(x =>
                x.NoEmpleado.Contains(term) ||
                x.PrimerNombre.Contains(term) ||
                x.PrimerApellido.Contains(term) ||
                (x.SegundoNombre != null && x.SegundoNombre.Contains(term)) ||
                (x.SegundoApellido != null && x.SegundoApellido.Contains(term)));
        }

        return query;
    }

    private IQueryable<Colaborador> NodeCollaboratorsQuery(OrganigramaNodo node)
    {
        var query = db.Colaboradores
            .Include(x => x.Empresa)
            .Include(x => x.Departamento)
            .Include(x => x.Cargo)
            .Include(x => x.Estatus)
            .AsNoTracking()
            .Where(x => x.IsActive && OperationalStatusCodes.Contains(x.Estatus.Codigo));

        if (node.EmpresaId.HasValue)
        {
            query = query.Where(x => x.EmpresaId == node.EmpresaId.Value);
        }

        if (node.DepartamentoId.HasValue)
        {
            query = query.Where(x => x.DepartamentoId == node.DepartamentoId.Value);
        }

        if (node.CargoId.HasValue)
        {
            query = query.Where(x => x.CargoId == node.CargoId.Value);
        }

        return query;
    }

    private async Task<bool> WouldCreateCycleAsync(int nodeId, int proposedParentId, CancellationToken cancellationToken)
    {
        var currentParentId = proposedParentId;
        while (currentParentId != 0)
        {
            if (currentParentId == nodeId)
            {
                return true;
            }

            var parent = await db.OrganigramaNodos
                .AsNoTracking()
                .Where(x => x.OrganigramaNodoId == currentParentId)
                .Select(x => x.NodoPadreId)
                .FirstOrDefaultAsync(cancellationToken);

            currentParentId = parent ?? 0;
        }

        return false;
    }

    private DepartamentoResponsableDto ToResponsableDto(DepartamentoResponsable responsable)
    {
        var warnings = GetResponsableWarnings(responsable);
        return new DepartamentoResponsableDto(
            responsable.DepartamentoResponsableId,
            responsable.EmpresaId,
            responsable.Empresa.Nombre,
            responsable.DepartamentoId,
            responsable.Departamento.Nombre,
            responsable.ColaboradorResponsableId,
            responsable.ColaboradorResponsable.NoEmpleado,
            responsable.ColaboradorResponsable.NombreCompleto(),
            responsable.UsuarioResponsableId,
            responsable.UsuarioResponsable?.NombreUsuario,
            responsable.TipoResponsable,
            responsable.EsPrincipal,
            responsable.PuedeAprobarSolicitudes,
            responsable.FechaInicio,
            responsable.FechaFin,
            responsable.Observacion,
            responsable.IsActive,
            warnings);
    }

    private AprobadorSolicitudDto ToAprobadorDto(DepartamentoResponsable responsable)
    {
        return new AprobadorSolicitudDto(
            responsable.DepartamentoResponsableId,
            responsable.EmpresaId,
            responsable.Empresa.Nombre,
            responsable.DepartamentoId,
            responsable.Departamento.Nombre,
            responsable.ColaboradorResponsableId,
            responsable.ColaboradorResponsable.NoEmpleado,
            responsable.ColaboradorResponsable.NombreCompleto(),
            responsable.ColaboradorResponsable.Cargo.Nombre,
            responsable.TipoResponsable,
            responsable.UsuarioResponsableId,
            responsable.UsuarioResponsable?.NombreUsuario,
            responsable.EsPrincipal,
            GetResponsableWarnings(responsable));
    }

    private static IReadOnlyList<string> GetResponsableWarnings(DepartamentoResponsable responsable)
    {
        var warnings = new List<string>();
        if (responsable.ColaboradorResponsable.EmpresaId != responsable.EmpresaId)
        {
            warnings.Add("El colaborador pertenece a otra empresa.");
        }

        if (responsable.ColaboradorResponsable.DepartamentoId != responsable.DepartamentoId)
        {
            warnings.Add("El colaborador pertenece a otro departamento.");
        }

        return warnings;
    }

    private void AddHistory(string entity, int entityId, string action, string? before, string? after, string? comment)
    {
        db.OrganigramaHistorialCambios.Add(new OrganigramaHistorialCambio
        {
            Entidad = entity,
            EntidadId = entityId,
            Accion = action,
            ValorAnterior = before,
            ValorNuevo = after,
            UsuarioId = User.CurrentUserId(),
            Fecha = DateTime.UtcNow,
            Comentario = comment
        });
    }

    private static string NormalizeResponsibleType(string value)
    {
        var match = ValidResponsibleTypes.FirstOrDefault(x => x.Equals(value.Trim(), StringComparison.OrdinalIgnoreCase));
        return match ?? "Otro";
    }

    private static string ToJson(object value)
    {
        return JsonSerializer.Serialize(value, new JsonSerializerOptions { WriteIndented = false });
    }

    private static bool IsOperationalCollaborator(Colaborador colaborador)
    {
        return colaborador.IsActive && OperationalStatusCodes.Contains(colaborador.Estatus.Codigo);
    }

    private string CurrentUserName()
    {
        return User.Identity?.Name ?? $"Usuario {User.CurrentUserId()}";
    }

    private bool CanReadOrganigrama()
    {
        return User.IsInRole(AppRoles.Admin) || User.IsInRole(AppRoles.RRHH);
    }
}
