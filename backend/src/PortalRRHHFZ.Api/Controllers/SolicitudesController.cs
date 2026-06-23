using System.Globalization;
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
[Authorize(Policy = AppPolicies.RequireSolicitudes)]
[Route("api/solicitudes")]
public sealed class SolicitudesController(AppDbContext db) : ControllerBase
{
    private static readonly EstadoSolicitud[] EditableStates = [EstadoSolicitud.Borrador, EstadoSolicitud.Devuelta];
    private static readonly EstadoSolicitud[] TerminalStates =
        [EstadoSolicitud.Aprobada, EstadoSolicitud.Rechazada, EstadoSolicitud.Cancelada, EstadoSolicitud.Cerrada, EstadoSolicitud.Ejecutada];
    private static readonly string[] OperationalStatusCodes = ["A", "V", "S"];
    private static readonly TipoAccionPersonal[] ActionsRequiringCollaborator =
    [
        TipoAccionPersonal.Vacaciones,
        TipoAccionPersonal.AjusteSalario,
        TipoAccionPersonal.CambioPosicion,
        TipoAccionPersonal.TrasladoCambioArea,
        TipoAccionPersonal.Licencia,
        TipoAccionPersonal.FinalizacionDesvinculacion,
        TipoAccionPersonal.RenovacionExtensionContrato,
        TipoAccionPersonal.ContinuidadLaboral
    ];
    private static readonly string[] EvaluationValues = ["Deficiente", "Regular", "Bueno", "Excelente"];

    private sealed record ApproverSelection(int? UsuarioId, int? ColaboradorId, int? DepartamentoResponsableId)
    {
        public bool HasLeader => UsuarioId.HasValue || ColaboradorId.HasValue || DepartamentoResponsableId.HasValue;
    }

    private sealed record ExecutionResult(bool Success, string Message)
    {
        public static ExecutionResult Ok(string message) => new(true, message);
        public static ExecutionResult Fail(string message) => new(false, message);
    }

    [HttpGet("tipos")]
    public IActionResult Tipos()
    {
        var data = new List<TipoSolicitudDisponibleDto>
        {
            new(TipoSolicitud.RequisicionPersonal.ToString(), "Requisicion de Personal", true, "Disponible"),
            new(TipoSolicitud.AccionPersonal.ToString(), "Accion de Personal", true, "Disponible"),
            new(TipoSolicitud.Vacaciones.ToString(), "Solicitud de Vacaciones", false, "Proximamente")
        };

        return Ok(ApiResponse<List<TipoSolicitudDisponibleDto>>.Ok(data));
    }

    [HttpGet("accion-personal/tipos")]
    public IActionResult TiposAccionPersonal()
    {
        var data = Enum.GetValues<TipoAccionPersonal>()
            .Select(x => new TipoAccionPersonalDto(x.ToString(), FormatTipoAccion(x), ActionsRequiringCollaborator.Contains(x)))
            .ToList();

        return Ok(ApiResponse<List<TipoAccionPersonalDto>>.Ok(data));
    }

    [HttpGet("aprobadores-lideres")]
    public async Task<IActionResult> AprobadoresLideres(CancellationToken cancellationToken)
    {
        var roles = new[] { AppRoles.Supervisor, AppRoles.RRHH, AppRoles.Admin };
        var data = await db.Usuarios
            .Include(x => x.Rol)
            .AsNoTracking()
            .Where(x => x.IsActive && roles.Contains(x.Rol.Nombre))
            .OrderBy(x => x.Rol.Nombre == AppRoles.Supervisor ? 0 : 1)
            .ThenBy(x => x.NombreUsuario)
            .Select(x => x.ToDto())
            .ToListAsync(cancellationToken);

        return Ok(ApiResponse<List<UsuarioDto>>.Ok(data));
    }

    [HttpGet]
    public async Task<IActionResult> Get(
        [FromQuery] string? tipo,
        [FromQuery] string? estado,
        [FromQuery] int? empresaId,
        [FromQuery] int? departamentoId,
        [FromQuery] DateTime? fechaDesde,
        [FromQuery] DateTime? fechaHasta,
        CancellationToken cancellationToken)
    {
        var query = db.Solicitudes
            .Include(x => x.SolicitanteUsuario)
            .Include(x => x.Empresa)
            .Include(x => x.Departamento)
            .Include(x => x.Aprobaciones)
            .AsNoTracking()
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(tipo))
        {
            if (!Enum.TryParse<TipoSolicitud>(tipo, true, out var tipoSolicitud))
            {
                return BadRequest(ApiResponse<object>.Fail("Tipo de solicitud no valido."));
            }

            query = query.Where(x => x.TipoSolicitud == tipoSolicitud);
        }

        if (!string.IsNullOrWhiteSpace(estado))
        {
            if (!Enum.TryParse<EstadoSolicitud>(estado, true, out var estadoSolicitud))
            {
                return BadRequest(ApiResponse<object>.Fail("Estado de solicitud no valido."));
            }

            query = query.Where(x => x.Estado == estadoSolicitud);
        }

        if (empresaId.HasValue)
        {
            query = query.Where(x => x.EmpresaId == empresaId.Value);
        }

        if (departamentoId.HasValue)
        {
            query = query.Where(x => x.DepartamentoId == departamentoId.Value);
        }

        if (fechaDesde.HasValue)
        {
            query = query.Where(x => x.FechaSolicitud.Date >= fechaDesde.Value.Date);
        }

        if (fechaHasta.HasValue)
        {
            query = query.Where(x => x.FechaSolicitud.Date <= fechaHasta.Value.Date);
        }

        if (!CanManageAll())
        {
            var currentUserId = User.CurrentUserId();
            query = query.Where(x => x.SolicitanteUsuarioId == currentUserId || x.Aprobaciones.Any(a => a.UsuarioAprobadorId == currentUserId));
        }

        var solicitudes = await query
            .OrderByDescending(x => x.FechaSolicitud)
            .ThenByDescending(x => x.SolicitudId)
            .ToListAsync(cancellationToken);

        var data = solicitudes.Select(ToListDto).ToList();
        return Ok(ApiResponse<List<SolicitudListDto>>.Ok(data));
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id, CancellationToken cancellationToken)
    {
        var solicitud = await LoadSolicitudAsync(id, true, cancellationToken);
        if (solicitud is null)
        {
            return NotFound(ApiResponse<object>.Fail("Solicitud no encontrada."));
        }

        if (!CanView(solicitud))
        {
            return Forbid();
        }

        return Ok(ApiResponse<SolicitudDetailDto>.Ok(ToDetailDto(solicitud)));
    }

    [HttpPost("requisicion-personal")]
    public async Task<IActionResult> CreateRequisicion([FromBody] CreateRequisicionPersonalRequest request, CancellationToken cancellationToken)
    {
        var validation = await ValidateRequisicionAsync(request, request.Enviar && !CanReviewRRHH(), cancellationToken);
        if (validation is not null)
        {
            return BadRequest(ApiResponse<object>.Fail(validation));
        }

        var approver = await ResolveApproverAsync(request, cancellationToken);
        var initialState = request.Enviar
            ? GetSubmittedState(approver)
            : EstadoSolicitud.Borrador;

        var solicitud = new Solicitud
        {
            CodigoSolicitud = await GenerateCodigoSolicitudAsync(cancellationToken),
            TipoSolicitud = TipoSolicitud.RequisicionPersonal,
            Estado = initialState,
            SolicitanteUsuarioId = User.CurrentUserId(),
            FechaSolicitud = DateTime.UtcNow,
            CreatedBy = User.Identity?.Name
        };

        ApplySolicitud(solicitud, request);
        solicitud.RequisicionPersonal = BuildRequisicion(request);
        solicitud.RequisicionPersonal.SolicitadoPorTexto = CurrentUserName();
        SetApprovalFlow(solicitud, approver, initialState);
        AddHistorial(solicitud, "CREACION", null, initialState, request.Enviar ? "Creacion y envio de requisicion de personal." : "Creacion de requisicion de personal.");

        db.Solicitudes.Add(solicitud);
        await db.SaveChangesAsync(cancellationToken);

        return CreatedAtAction(
            nameof(GetById),
            new { id = solicitud.SolicitudId },
            ApiResponse<object>.Ok(new { solicitud.SolicitudId, solicitud.CodigoSolicitud }, request.Enviar ? "Requisicion enviada." : "Requisicion guardada como borrador."));
    }

    [HttpPut("requisicion-personal/{id:int}")]
    public async Task<IActionResult> UpdateRequisicion(int id, [FromBody] UpdateRequisicionPersonalRequest request, CancellationToken cancellationToken)
    {
        var solicitud = await LoadSolicitudAsync(id, false, cancellationToken);
        if (solicitud is null || solicitud.TipoSolicitud != TipoSolicitud.RequisicionPersonal)
        {
            return NotFound(ApiResponse<object>.Fail("Requisicion no encontrada."));
        }

        if (!CanEdit(solicitud))
        {
            return Forbid();
        }

        if (!EditableStates.Contains(solicitud.Estado))
        {
            return BadRequest(ApiResponse<object>.Fail("Solo se pueden editar solicitudes en borrador o devueltas."));
        }

        var validation = await ValidateRequisicionAsync(request, false, cancellationToken);
        if (validation is not null)
        {
            return BadRequest(ApiResponse<object>.Fail(validation));
        }

        var approver = await ResolveApproverAsync(request, cancellationToken);
        ApplySolicitud(solicitud, request);
        ApplyRequisicion(solicitud.RequisicionPersonal!, request);
        solicitud.RequisicionPersonal!.SolicitadoPorTexto = solicitud.SolicitanteUsuario?.NombreUsuario ?? CurrentUserName();
        SetApprovalFlow(solicitud, approver, solicitud.Estado);
        solicitud.UpdatedBy = User.Identity?.Name;
        AddHistorial(solicitud, "ACTUALIZACION", solicitud.Estado, solicitud.Estado, "Actualizacion manual de requisicion de personal.");

        await db.SaveChangesAsync(cancellationToken);
        return Ok(ApiResponse<object>.Ok(new { solicitud.SolicitudId, solicitud.CodigoSolicitud }, "Requisicion actualizada."));
    }

    [HttpPost("accion-personal")]
    public async Task<IActionResult> CreateAccionPersonal([FromBody] CreateAccionPersonalRequest request, CancellationToken cancellationToken)
    {
        if (!TryParseTipoAccion(request.TipoAccion, out var tipoAccion))
        {
            return BadRequest(ApiResponse<object>.Fail("Tipo de accion de personal no valido."));
        }

        var validation = await ValidateAccionPersonalAsync(request, tipoAccion, request.Enviar && !CanReviewRRHH(), cancellationToken);
        if (validation is not null)
        {
            return BadRequest(ApiResponse<object>.Fail(validation));
        }

        var accion = await BuildAccionPersonalAsync(request, tipoAccion, cancellationToken);
        var approver = await ResolveApproverAsync(request.DepartamentoResponsableId, request.LiderAprobadorUsuarioId, request.LiderAprobadorColaboradorId, cancellationToken);
        var initialState = request.Enviar
            ? GetSubmittedState(approver)
            : EstadoSolicitud.Borrador;

        var solicitud = new Solicitud
        {
            CodigoSolicitud = await GenerateCodigoSolicitudAsync(TipoSolicitud.AccionPersonal, cancellationToken),
            TipoSolicitud = TipoSolicitud.AccionPersonal,
            Estado = initialState,
            SolicitanteUsuarioId = User.CurrentUserId(),
            FechaSolicitud = DateTime.UtcNow,
            CreatedBy = User.Identity?.Name
        };

        ApplySolicitud(solicitud, request, accion);
        solicitud.AccionPersonal = accion;
        SetApprovalFlow(solicitud, approver, initialState);
        AddHistorial(solicitud, "CREACION", null, initialState, request.Enviar ? "Creacion y envio de accion de personal." : "Creacion de accion de personal.");

        db.Solicitudes.Add(solicitud);
        await db.SaveChangesAsync(cancellationToken);

        return CreatedAtAction(
            nameof(GetById),
            new { id = solicitud.SolicitudId },
            ApiResponse<object>.Ok(new { solicitud.SolicitudId, solicitud.CodigoSolicitud }, request.Enviar ? "Accion de personal enviada." : "Accion de personal guardada como borrador."));
    }

    [HttpPut("accion-personal/{id:int}")]
    public async Task<IActionResult> UpdateAccionPersonal(int id, [FromBody] UpdateAccionPersonalRequest request, CancellationToken cancellationToken)
    {
        if (!TryParseTipoAccion(request.TipoAccion, out var tipoAccion))
        {
            return BadRequest(ApiResponse<object>.Fail("Tipo de accion de personal no valido."));
        }

        var solicitud = await LoadSolicitudAsync(id, false, cancellationToken);
        if (solicitud is null || solicitud.TipoSolicitud != TipoSolicitud.AccionPersonal || solicitud.AccionPersonal is null)
        {
            return NotFound(ApiResponse<object>.Fail("Accion de personal no encontrada."));
        }

        if (!CanEdit(solicitud))
        {
            return Forbid();
        }

        if (!EditableStates.Contains(solicitud.Estado))
        {
            return BadRequest(ApiResponse<object>.Fail("Solo se pueden editar solicitudes en borrador o devueltas."));
        }

        var validation = await ValidateAccionPersonalAsync(request, tipoAccion, false, cancellationToken);
        if (validation is not null)
        {
            return BadRequest(ApiResponse<object>.Fail(validation));
        }

        var approver = await ResolveApproverAsync(request.DepartamentoResponsableId, request.LiderAprobadorUsuarioId, request.LiderAprobadorColaboradorId, cancellationToken);
        await ApplyAccionPersonalAsync(solicitud.AccionPersonal, request, tipoAccion, cancellationToken);
        ApplySolicitud(solicitud, request, solicitud.AccionPersonal);
        SetApprovalFlow(solicitud, approver, solicitud.Estado);
        solicitud.UpdatedBy = User.Identity?.Name;
        AddHistorial(solicitud, "ACTUALIZACION", solicitud.Estado, solicitud.Estado, "Actualizacion manual de accion de personal.");

        await db.SaveChangesAsync(cancellationToken);
        return Ok(ApiResponse<object>.Ok(new { solicitud.SolicitudId, solicitud.CodigoSolicitud }, "Accion de personal actualizada."));
    }

    [HttpGet("accion-personal/{id:int}")]
    public async Task<IActionResult> GetAccionPersonal(int id, CancellationToken cancellationToken)
    {
        var solicitud = await LoadSolicitudAsync(id, true, cancellationToken);
        if (solicitud is null || solicitud.TipoSolicitud != TipoSolicitud.AccionPersonal)
        {
            return NotFound(ApiResponse<object>.Fail("Accion de personal no encontrada."));
        }

        if (!CanView(solicitud))
        {
            return Forbid();
        }

        return Ok(ApiResponse<SolicitudDetailDto>.Ok(ToDetailDto(solicitud)));
    }

    [HttpPost("accion-personal/{id:int}/ejecutar")]
    public async Task<IActionResult> EjecutarAccionPersonal(int id, [FromBody] EjecutarAccionPersonalRequest? request, CancellationToken cancellationToken)
    {
        var solicitud = await LoadSolicitudAsync(id, false, cancellationToken);
        if (solicitud is null || solicitud.TipoSolicitud != TipoSolicitud.AccionPersonal || solicitud.AccionPersonal is null)
        {
            return NotFound(ApiResponse<object>.Fail("Accion de personal no encontrada."));
        }

        if (!CanReviewRRHH())
        {
            return Forbid();
        }

        if (solicitud.Estado != EstadoSolicitud.Aprobada)
        {
            return BadRequest(ApiResponse<object>.Fail("Solo se pueden ejecutar acciones de personal aprobadas."));
        }

        if (solicitud.AccionPersonal.Ejecutada)
        {
            return BadRequest(ApiResponse<object>.Fail("La accion de personal ya fue ejecutada."));
        }

        var before = solicitud.Estado;
        var execution = await ExecuteAccionPersonalAsync(solicitud, request?.Comentario, cancellationToken);
        if (!execution.Success)
        {
            await db.SaveChangesAsync(cancellationToken);
            return BadRequest(ApiResponse<object>.Fail(execution.Message));
        }

        solicitud.Estado = EstadoSolicitud.Ejecutada;
        solicitud.UpdatedBy = User.Identity?.Name;
        AddHistorial(solicitud, "EJECUTAR_ACCION", before, solicitud.Estado, execution.Message);

        await db.SaveChangesAsync(cancellationToken);
        return Ok(ApiResponse<SolicitudDetailDto>.Ok(await ReloadDetailDtoAsync(id, cancellationToken), execution.Message));
    }

    [HttpPost("{id:int}/enviar")]
    public async Task<IActionResult> Enviar(int id, [FromBody] EnviarSolicitudRequest? request, CancellationToken cancellationToken)
    {
        var solicitud = await LoadSolicitudAsync(id, false, cancellationToken);
        if (solicitud is null)
        {
            return NotFound(ApiResponse<object>.Fail("Solicitud no encontrada."));
        }

        if (!CanEdit(solicitud))
        {
            return Forbid();
        }

        if (!EditableStates.Contains(solicitud.Estado))
        {
            return BadRequest(ApiResponse<object>.Fail("Solo se pueden enviar solicitudes en borrador o devueltas."));
        }

        var before = solicitud.Estado;
        var approver = ToApproverSelection(GetLeaderApproval(solicitud));
        if (!approver.HasLeader && !CanReviewRRHH())
        {
            return BadRequest(ApiResponse<object>.Fail("Debe seleccionar un aprobador configurado en Organigrama antes de enviar."));
        }

        solicitud.Estado = GetSubmittedState(approver);
        SetApprovalFlow(solicitud, approver, solicitud.Estado);
        solicitud.UpdatedBy = User.Identity?.Name;
        AddHistorial(solicitud, "ENVIAR", before, solicitud.Estado, request?.Comentario);

        await db.SaveChangesAsync(cancellationToken);
        return Ok(ApiResponse<SolicitudDetailDto>.Ok(await ReloadDetailDtoAsync(id, cancellationToken), "Solicitud enviada."));
    }

    [HttpPost("{id:int}/aprobar")]
    public async Task<IActionResult> Aprobar(int id, [FromBody] DecidirSolicitudRequest? request, CancellationToken cancellationToken)
    {
        var solicitud = await LoadSolicitudAsync(id, false, cancellationToken);
        if (solicitud is null)
        {
            return NotFound(ApiResponse<object>.Fail("Solicitud no encontrada."));
        }

        if (solicitud.Estado != EstadoSolicitud.PendienteAprobacionLider)
        {
            return BadRequest(ApiResponse<object>.Fail("La solicitud no esta pendiente de aprobacion del lider."));
        }

        if (!CanApproveLeader(solicitud))
        {
            return Forbid();
        }

        var approval = GetLeaderApproval(solicitud);
        if (approval is null || approval.Estado != EstadoAprobacion.Pendiente)
        {
            return BadRequest(ApiResponse<object>.Fail("No hay una aprobacion de lider pendiente."));
        }

        var before = solicitud.Estado;
        MarkApproval(approval, EstadoAprobacion.Aprobada, request?.Comentario);
        if (solicitud.RequisicionPersonal is not null)
        {
            solicitud.RequisicionPersonal.AutorizadoPorTexto = CurrentUserName();
            solicitud.RequisicionPersonal.FechaAutorizacion = DateTime.UtcNow;
        }

        solicitud.Estado = EstadoSolicitud.PendienteRevisionRRHH;
        var rrhhApproval = GetRRHHApproval(solicitud);
        if (rrhhApproval is not null)
        {
            rrhhApproval.Estado = EstadoAprobacion.Pendiente;
            rrhhApproval.FechaDecision = null;
            rrhhApproval.Comentario = null;
        }

        solicitud.UpdatedBy = User.Identity?.Name;
        AddHistorial(solicitud, "APROBAR", before, solicitud.Estado, request?.Comentario);

        await db.SaveChangesAsync(cancellationToken);
        return Ok(ApiResponse<SolicitudDetailDto>.Ok(await ReloadDetailDtoAsync(id, cancellationToken), "Solicitud aprobada por lider."));
    }

    [HttpPost("{id:int}/confirmar-rrhh")]
    public async Task<IActionResult> ConfirmarRRHH(int id, [FromBody] DecidirSolicitudRequest? request, CancellationToken cancellationToken)
    {
        var solicitud = await LoadSolicitudAsync(id, false, cancellationToken);
        if (solicitud is null)
        {
            return NotFound(ApiResponse<object>.Fail("Solicitud no encontrada."));
        }

        if (!CanReviewRRHH())
        {
            return Forbid();
        }

        if (solicitud.Estado != EstadoSolicitud.PendienteRevisionRRHH)
        {
            return BadRequest(ApiResponse<object>.Fail("La solicitud no esta pendiente de revision de RRHH."));
        }

        var before = solicitud.Estado;
        var approval = GetRRHHApproval(solicitud);
        if (approval is not null)
        {
            MarkApproval(approval, EstadoAprobacion.Aprobada, request?.Comentario);
        }

        solicitud.Estado = EstadoSolicitud.Aprobada;
        solicitud.UpdatedBy = User.Identity?.Name;
        AddHistorial(solicitud, "CONFIRMAR_RRHH", before, solicitud.Estado, request?.Comentario);

        await db.SaveChangesAsync(cancellationToken);
        return Ok(ApiResponse<SolicitudDetailDto>.Ok(await ReloadDetailDtoAsync(id, cancellationToken), "Solicitud confirmada por RRHH."));
    }

    [HttpPost("{id:int}/rechazar")]
    public Task<IActionResult> Rechazar(int id, [FromBody] DecidirSolicitudRequest? request, CancellationToken cancellationToken)
        => Decide(id, "RECHAZAR", EstadoAprobacion.Rechazada, EstadoSolicitud.Rechazada, request?.Comentario, cancellationToken);

    [HttpPost("{id:int}/devolver")]
    public Task<IActionResult> Devolver(int id, [FromBody] DecidirSolicitudRequest? request, CancellationToken cancellationToken)
        => Decide(id, "DEVOLVER", EstadoAprobacion.Devuelta, EstadoSolicitud.Devuelta, request?.Comentario, cancellationToken);

    [HttpPost("{id:int}/cancelar")]
    public async Task<IActionResult> Cancelar(int id, [FromBody] DecidirSolicitudRequest? request, CancellationToken cancellationToken)
    {
        var solicitud = await LoadSolicitudAsync(id, false, cancellationToken);
        if (solicitud is null)
        {
            return NotFound(ApiResponse<object>.Fail("Solicitud no encontrada."));
        }

        if (!CanEdit(solicitud))
        {
            return Forbid();
        }

        if (TerminalStates.Contains(solicitud.Estado))
        {
            return BadRequest(ApiResponse<object>.Fail("La solicitud ya esta en un estado final."));
        }

        var before = solicitud.Estado;
        solicitud.Estado = EstadoSolicitud.Cancelada;
        solicitud.UpdatedBy = User.Identity?.Name;
        AddHistorial(solicitud, "CANCELAR", before, solicitud.Estado, request?.Comentario);

        await db.SaveChangesAsync(cancellationToken);
        return Ok(ApiResponse<SolicitudDetailDto>.Ok(await ReloadDetailDtoAsync(id, cancellationToken), "Solicitud cancelada."));
    }

    [HttpPost("{id:int}/cerrar")]
    public async Task<IActionResult> Cerrar(int id, [FromBody] DecidirSolicitudRequest? request, CancellationToken cancellationToken)
    {
        var solicitud = await LoadSolicitudAsync(id, false, cancellationToken);
        if (solicitud is null)
        {
            return NotFound(ApiResponse<object>.Fail("Solicitud no encontrada."));
        }

        if (!CanReviewRRHH())
        {
            return Forbid();
        }

        if (solicitud.Estado != EstadoSolicitud.Aprobada)
        {
            return BadRequest(ApiResponse<object>.Fail("Solo se pueden cerrar solicitudes aprobadas."));
        }

        var before = solicitud.Estado;
        solicitud.Estado = EstadoSolicitud.Cerrada;
        solicitud.UpdatedBy = User.Identity?.Name;
        AddHistorial(solicitud, "CERRAR", before, solicitud.Estado, request?.Comentario);

        await db.SaveChangesAsync(cancellationToken);
        return Ok(ApiResponse<SolicitudDetailDto>.Ok(await ReloadDetailDtoAsync(id, cancellationToken), "Solicitud cerrada."));
    }

    private async Task<IActionResult> Decide(
        int id,
        string action,
        EstadoAprobacion approvalState,
        EstadoSolicitud targetState,
        string? comentario,
        CancellationToken cancellationToken)
    {
        var solicitud = await LoadSolicitudAsync(id, false, cancellationToken);
        if (solicitud is null)
        {
            return NotFound(ApiResponse<object>.Fail("Solicitud no encontrada."));
        }

        var approval = GetPendingApprovalForCurrentStage(solicitud);
        if (approval is null)
        {
            return BadRequest(ApiResponse<object>.Fail("La solicitud no tiene una aprobacion pendiente para esta accion."));
        }

        if (!CanDecideCurrentStage(solicitud))
        {
            return Forbid();
        }

        var before = solicitud.Estado;
        MarkApproval(approval, approvalState, comentario);
        solicitud.Estado = targetState;
        solicitud.UpdatedBy = User.Identity?.Name;
        AddHistorial(solicitud, action, before, targetState, comentario);

        await db.SaveChangesAsync(cancellationToken);
        return Ok(ApiResponse<SolicitudDetailDto>.Ok(await ReloadDetailDtoAsync(id, cancellationToken), targetState == EstadoSolicitud.Devuelta ? "Solicitud devuelta." : "Solicitud rechazada."));
    }

    private async Task<SolicitudDetailDto> ReloadDetailDtoAsync(int id, CancellationToken cancellationToken)
    {
        var updated = await LoadSolicitudAsync(id, true, cancellationToken);
        return ToDetailDto(updated!);
    }

    private async Task<Solicitud?> LoadSolicitudAsync(int id, bool asNoTracking, CancellationToken cancellationToken)
    {
        var query = db.Solicitudes
            .AsSplitQuery()
            .Include(x => x.SolicitanteUsuario)
            .Include(x => x.Empresa)
            .Include(x => x.Departamento)
            .Include(x => x.Cargo)
            .Include(x => x.Colaborador)
            .Include(x => x.RequisicionPersonal!).ThenInclude(x => x.DepartamentoSolicitado)
            .Include(x => x.RequisicionPersonal!).ThenInclude(x => x.ColaboradorReemplazado)
            .Include(x => x.RequisicionPersonal!).ThenInclude(x => x.TipoContrato)
            .Include(x => x.AccionPersonal!).ThenInclude(x => x.Colaborador)
            .Include(x => x.AccionPersonal!).ThenInclude(x => x.EmpresaActual)
            .Include(x => x.AccionPersonal!).ThenInclude(x => x.DepartamentoActual)
            .Include(x => x.AccionPersonal!).ThenInclude(x => x.CargoActual)
            .Include(x => x.AccionPersonal!).ThenInclude(x => x.JefeActual)
            .Include(x => x.AccionPersonal!).ThenInclude(x => x.TipoContratoActual)
            .Include(x => x.AccionPersonal!).ThenInclude(x => x.EstatusActual)
            .Include(x => x.AccionPersonal!).ThenInclude(x => x.TipoContratoNuevo)
            .Include(x => x.AccionPersonal!).ThenInclude(x => x.CargoNuevo)
            .Include(x => x.AccionPersonal!).ThenInclude(x => x.DepartamentoNuevo)
            .Include(x => x.AccionPersonal!).ThenInclude(x => x.EmpresaNueva)
            .Include(x => x.AccionPersonal!).ThenInclude(x => x.JefeNuevo)
            .Include(x => x.AccionPersonal!).ThenInclude(x => x.CargoTrasladoActual)
            .Include(x => x.AccionPersonal!).ThenInclude(x => x.CargoTrasladoNuevo)
            .Include(x => x.AccionPersonal!).ThenInclude(x => x.DepartamentoTrasladoActual)
            .Include(x => x.AccionPersonal!).ThenInclude(x => x.DepartamentoTrasladoNuevo)
            .Include(x => x.AccionPersonal!).ThenInclude(x => x.EmpresaTrasladoActual)
            .Include(x => x.AccionPersonal!).ThenInclude(x => x.EmpresaTrasladoNueva)
            .Include(x => x.AccionPersonal!).ThenInclude(x => x.JefeTrasladoNuevo)
            .Include(x => x.AccionPersonal!).ThenInclude(x => x.MotivoSalida)
            .Include(x => x.AccionPersonal!).ThenInclude(x => x.EjecutadaPorUsuario)
            .Include(x => x.AccionPersonal!).ThenInclude(x => x.CambiosAplicados).ThenInclude(x => x.Usuario)
            .Include(x => x.Aprobaciones).ThenInclude(x => x.UsuarioAprobador)
            .Include(x => x.Aprobaciones).ThenInclude(x => x.ColaboradorAprobador)
            .Include(x => x.Aprobaciones).ThenInclude(x => x.DepartamentoResponsable)
            .Include(x => x.Historial).ThenInclude(x => x.Usuario)
            .Where(x => x.SolicitudId == id);

        if (asNoTracking)
        {
            query = query.AsNoTracking();
        }

        return await query.FirstOrDefaultAsync(cancellationToken);
    }

    private async Task<string?> ValidateRequisicionAsync(RequisicionPersonalRequestBase request, bool requireApprover, CancellationToken cancellationToken)
    {
        if (!request.EmpresaId.HasValue)
        {
            return "Empresa es obligatoria.";
        }

        if (!await db.Empresas.AnyAsync(x => x.EmpresaId == request.EmpresaId.Value && x.IsActive, cancellationToken))
        {
            return "Empresa no valida.";
        }

        if (request.DepartamentoSolicitadoId.HasValue)
        {
            var departamento = await db.Departamentos
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.DepartamentoId == request.DepartamentoSolicitadoId.Value && x.IsActive, cancellationToken);

            if (departamento is null || departamento.EmpresaId != request.EmpresaId.Value)
            {
                return "Departamento solicitado no pertenece a la empresa seleccionada.";
            }
        }

        if (string.IsNullOrWhiteSpace(request.CargoSolicitado))
        {
            return "Cargo solicitado es obligatorio.";
        }

        if (request.NumeroPlazas <= 0)
        {
            return "Numero de plazas debe ser mayor que cero.";
        }

        if (request.EsPosicionNueva && request.EsReemplazo)
        {
            return "La requisicion no puede ser posicion nueva y reemplazo al mismo tiempo.";
        }

        if (request.EsReemplazo && !request.ColaboradorReemplazadoId.HasValue && string.IsNullOrWhiteSpace(request.NombrePersonaReemplazada))
        {
            return "Debe indicar la persona reemplazada.";
        }

        if (request.ColaboradorReemplazadoId.HasValue && !await db.Colaboradores.AnyAsync(x => x.ColaboradorId == request.ColaboradorReemplazadoId.Value, cancellationToken))
        {
            return "Colaborador reemplazado no encontrado.";
        }

        if (request.TipoContratoId.HasValue && !await db.TiposContrato.AnyAsync(x => x.TipoContratoId == request.TipoContratoId.Value && x.IsActive, cancellationToken))
        {
            return "Tipo de contrato no valido.";
        }

        if (request.Salario is < 0 || request.GastoRepresentacion is < 0 || request.SalarioVariable is < 0)
        {
            return "Los montos salariales no pueden ser negativos.";
        }

        if (request.AniosExperiencia is < 0 || request.EdadMinima is < 0 || request.EdadMaxima is < 0)
        {
            return "Experiencia y edad no pueden ser negativas.";
        }

        if (request.EdadMinima.HasValue && request.EdadMaxima.HasValue && request.EdadMinima.Value > request.EdadMaxima.Value)
        {
            return "Edad minima no puede ser mayor que edad maxima.";
        }

        if (request.FechaAperturaProceso.HasValue && request.FechaEntregaCandidatos.HasValue && request.FechaEntregaCandidatos.Value.Date < request.FechaAperturaProceso.Value.Date)
        {
            return "Fecha de entrega de candidatos no puede ser anterior a la apertura del proceso.";
        }

        if (requireApprover && !HasRequestedApprover(request))
        {
            return "Debe seleccionar un aprobador configurado en Organigrama antes de enviar.";
        }

        if (request.DepartamentoResponsableId.HasValue)
        {
            var responsable = await db.DepartamentoResponsables
                .Include(x => x.ColaboradorResponsable).ThenInclude(x => x.Estatus)
                .Include(x => x.UsuarioResponsable).ThenInclude(x => x!.Rol)
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.DepartamentoResponsableId == request.DepartamentoResponsableId.Value, cancellationToken);

            if (responsable is null)
            {
                return "Responsable de organigrama no encontrado.";
            }

            if (!responsable.IsActive || responsable.FechaFin.HasValue && responsable.FechaFin.Value.Date < DateTime.Today)
            {
                return "Responsable de organigrama inactivo.";
            }

            if (!responsable.PuedeAprobarSolicitudes)
            {
                return "Responsable de organigrama no esta habilitado para aprobar solicitudes.";
            }

            if (request.EmpresaId.HasValue && responsable.EmpresaId != request.EmpresaId.Value)
            {
                return "El aprobador seleccionado no pertenece a la empresa de la solicitud.";
            }

            if (request.DepartamentoSolicitadoId.HasValue && responsable.DepartamentoId != request.DepartamentoSolicitadoId.Value)
            {
                return "El aprobador seleccionado no pertenece al departamento solicitado.";
            }

            if (!IsOperationalCollaborator(responsable.ColaboradorResponsable))
            {
                return "Colaborador responsable no esta activo para aprobaciones operativas.";
            }

            if (request.LiderAprobadorColaboradorId.HasValue && request.LiderAprobadorColaboradorId.Value != responsable.ColaboradorResponsableId)
            {
                return "El colaborador aprobador no coincide con el responsable seleccionado.";
            }

            if (request.LiderAprobadorUsuarioId.HasValue && responsable.UsuarioResponsableId.HasValue && request.LiderAprobadorUsuarioId.Value != responsable.UsuarioResponsableId.Value)
            {
                return "El usuario aprobador no coincide con el responsable seleccionado.";
            }

            if (responsable.UsuarioResponsable is not null &&
                (!responsable.UsuarioResponsable.IsActive || responsable.UsuarioResponsable.Rol.Nombre is not (AppRoles.Supervisor or AppRoles.RRHH or AppRoles.Admin)))
            {
                return "Usuario responsable no tiene permisos para aprobar solicitudes.";
            }
        }

        if (request.LiderAprobadorColaboradorId.HasValue)
        {
            var colaborador = await db.Colaboradores
                .Include(x => x.Estatus)
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.ColaboradorId == request.LiderAprobadorColaboradorId.Value, cancellationToken);

            if (colaborador is null)
            {
                return "Colaborador aprobador no encontrado.";
            }

            if (!IsOperationalCollaborator(colaborador))
            {
                return "Colaborador aprobador no esta activo para aprobaciones operativas.";
            }
        }

        if (request.LiderAprobadorUsuarioId.HasValue)
        {
            var lider = await db.Usuarios
                .Include(x => x.Rol)
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.UsuarioId == request.LiderAprobadorUsuarioId.Value, cancellationToken);

            if (lider is null)
            {
                return "Lider aprobador no encontrado.";
            }

            if (!lider.IsActive)
            {
                return "Lider aprobador inactivo.";
            }

            if (lider.Rol.Nombre is not (AppRoles.Supervisor or AppRoles.RRHH or AppRoles.Admin))
            {
                return "Lider aprobador no tiene permisos para aprobar solicitudes.";
            }
        }

        return null;
    }

    private async Task<string?> ValidateAccionPersonalAsync(AccionPersonalRequestBase request, TipoAccionPersonal tipoAccion, bool requireApprover, CancellationToken cancellationToken)
    {
        if (!request.FechaEfectiva.HasValue)
        {
            return "Fecha efectiva es obligatoria.";
        }

        if (string.IsNullOrWhiteSpace(request.Justificacion))
        {
            return "Justificacion es obligatoria.";
        }

        Colaborador? colaborador = null;
        if (ActionsRequiringCollaborator.Contains(tipoAccion) && !request.ColaboradorId.HasValue)
        {
            return "Debe seleccionar un colaborador para este tipo de accion.";
        }

        if (request.ColaboradorId.HasValue)
        {
            colaborador = await db.Colaboradores
                .Include(x => x.Estatus)
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.ColaboradorId == request.ColaboradorId.Value, cancellationToken);

            if (colaborador is null)
            {
                return "Colaborador relacionado no encontrado.";
            }

            if (!colaborador.IsActive)
            {
                return "Colaborador relacionado inactivo.";
            }

            if (ActionsRequiringCollaborator.Contains(tipoAccion) && !IsOperationalCollaborator(colaborador))
            {
                return "El colaborador seleccionado no puede estar cesante o suspendido para este tipo de accion.";
            }
        }

        var empresaSolicitudId = request.EmpresaId ?? colaborador?.EmpresaId;
        var departamentoSolicitudId = request.DepartamentoId ?? colaborador?.DepartamentoId;
        var cargoSolicitudId = request.CargoId ?? colaborador?.CargoId;

        if (!empresaSolicitudId.HasValue)
        {
            return "Empresa es obligatoria.";
        }

        if (!await db.Empresas.AnyAsync(x => x.EmpresaId == empresaSolicitudId.Value && x.IsActive, cancellationToken))
        {
            return "Empresa no valida.";
        }

        if (departamentoSolicitudId.HasValue)
        {
            var departamento = await db.Departamentos.AsNoTracking().FirstOrDefaultAsync(x => x.DepartamentoId == departamentoSolicitudId.Value && x.IsActive, cancellationToken);
            if (departamento is null || departamento.EmpresaId != empresaSolicitudId.Value)
            {
                return "Departamento no pertenece a la empresa seleccionada.";
            }
        }

        if (cargoSolicitudId.HasValue)
        {
            var cargo = await db.Cargos.AsNoTracking().FirstOrDefaultAsync(x => x.CargoId == cargoSolicitudId.Value && x.IsActive, cancellationToken);
            if (cargo is null || departamentoSolicitudId.HasValue && cargo.DepartamentoId != departamentoSolicitudId.Value)
            {
                return "Cargo no pertenece al departamento seleccionado.";
            }
        }

        var approverValidation = await ValidateApproverAsync(
            request.DepartamentoResponsableId,
            request.LiderAprobadorUsuarioId,
            request.LiderAprobadorColaboradorId,
            empresaSolicitudId,
            departamentoSolicitudId,
            requireApprover,
            cancellationToken);

        if (approverValidation is not null)
        {
            return approverValidation;
        }

        if (request.TipoContratoNuevoId.HasValue && !await db.TiposContrato.AnyAsync(x => x.TipoContratoId == request.TipoContratoNuevoId.Value && x.IsActive, cancellationToken))
        {
            return "Tipo de contrato nuevo no valido.";
        }

        if (request.MotivoSalidaId.HasValue && !await db.MotivosSalida.AnyAsync(x => x.MotivoSalidaId == request.MotivoSalidaId.Value && x.IsActive, cancellationToken))
        {
            return "Motivo de salida no valido.";
        }

        var relationValidation = await ValidateActionRelationsAsync(request, cancellationToken);
        if (relationValidation is not null)
        {
            return relationValidation;
        }

        if (request.SalarioNuevo is < 0 ||
            request.ViaticosNuevo is < 0 ||
            request.GastosRepresentacionNuevo is < 0 ||
            request.SalarioNuevoAjuste is < 0 ||
            request.AjustePorMes is < 0)
        {
            return "Los montos no pueden ser negativos.";
        }

        return tipoAccion switch
        {
            TipoAccionPersonal.Vacaciones => ValidateVacaciones(request),
            TipoAccionPersonal.ContratacionIngreso => ValidateContratacion(request),
            TipoAccionPersonal.AjusteSalario => ValidateAjusteSalario(request),
            TipoAccionPersonal.CambioPosicion => ValidateCambioPosicion(request),
            TipoAccionPersonal.TrasladoCambioArea => ValidateTraslado(request),
            TipoAccionPersonal.Licencia => ValidateLicencia(request),
            TipoAccionPersonal.FinalizacionDesvinculacion => ValidateFinalizacion(request),
            _ => null
        };
    }

    private async Task<string?> ValidateApproverAsync(
        int? departamentoResponsableId,
        int? liderAprobadorUsuarioId,
        int? liderAprobadorColaboradorId,
        int? empresaId,
        int? departamentoId,
        bool requireApprover,
        CancellationToken cancellationToken)
    {
        if (requireApprover && !HasRequestedApprover(departamentoResponsableId, liderAprobadorUsuarioId, liderAprobadorColaboradorId))
        {
            return "Debe seleccionar un aprobador configurado en Organigrama antes de enviar.";
        }

        if (departamentoResponsableId.HasValue)
        {
            var responsable = await db.DepartamentoResponsables
                .Include(x => x.ColaboradorResponsable).ThenInclude(x => x.Estatus)
                .Include(x => x.UsuarioResponsable).ThenInclude(x => x!.Rol)
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.DepartamentoResponsableId == departamentoResponsableId.Value, cancellationToken);

            if (responsable is null)
            {
                return "Responsable de organigrama no encontrado.";
            }

            if (!responsable.IsActive || responsable.FechaFin.HasValue && responsable.FechaFin.Value.Date < DateTime.Today)
            {
                return "Responsable de organigrama inactivo.";
            }

            if (!responsable.PuedeAprobarSolicitudes)
            {
                return "Responsable de organigrama no esta habilitado para aprobar solicitudes.";
            }

            if (empresaId.HasValue && responsable.EmpresaId != empresaId.Value)
            {
                return "El aprobador seleccionado no pertenece a la empresa de la solicitud.";
            }

            if (departamentoId.HasValue && responsable.DepartamentoId != departamentoId.Value)
            {
                return "El aprobador seleccionado no pertenece al departamento de la solicitud.";
            }

            if (!IsOperationalCollaborator(responsable.ColaboradorResponsable))
            {
                return "Colaborador responsable no esta activo para aprobaciones operativas.";
            }

            if (liderAprobadorColaboradorId.HasValue && liderAprobadorColaboradorId.Value != responsable.ColaboradorResponsableId)
            {
                return "El colaborador aprobador no coincide con el responsable seleccionado.";
            }

            if (liderAprobadorUsuarioId.HasValue && responsable.UsuarioResponsableId.HasValue && liderAprobadorUsuarioId.Value != responsable.UsuarioResponsableId.Value)
            {
                return "El usuario aprobador no coincide con el responsable seleccionado.";
            }

            if (responsable.UsuarioResponsable is not null &&
                (!responsable.UsuarioResponsable.IsActive || responsable.UsuarioResponsable.Rol.Nombre is not (AppRoles.Supervisor or AppRoles.RRHH or AppRoles.Admin)))
            {
                return "Usuario responsable no tiene permisos para aprobar solicitudes.";
            }
        }

        if (liderAprobadorColaboradorId.HasValue)
        {
            var colaborador = await db.Colaboradores
                .Include(x => x.Estatus)
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.ColaboradorId == liderAprobadorColaboradorId.Value, cancellationToken);

            if (colaborador is null)
            {
                return "Colaborador aprobador no encontrado.";
            }

            if (!IsOperationalCollaborator(colaborador))
            {
                return "Colaborador aprobador no esta activo para aprobaciones operativas.";
            }
        }

        if (liderAprobadorUsuarioId.HasValue)
        {
            var lider = await db.Usuarios
                .Include(x => x.Rol)
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.UsuarioId == liderAprobadorUsuarioId.Value, cancellationToken);

            if (lider is null)
            {
                return "Lider aprobador no encontrado.";
            }

            if (!lider.IsActive)
            {
                return "Lider aprobador inactivo.";
            }

            if (lider.Rol.Nombre is not (AppRoles.Supervisor or AppRoles.RRHH or AppRoles.Admin))
            {
                return "Lider aprobador no tiene permisos para aprobar solicitudes.";
            }
        }

        return null;
    }

    private async Task<string?> ValidateActionRelationsAsync(AccionPersonalRequestBase request, CancellationToken cancellationToken)
    {
        var validation = await ValidateEmpresaDepartamentoCargoAsync(request.EmpresaNuevaId, request.DepartamentoNuevoId, request.CargoNuevoId, "nuevo", cancellationToken);
        if (validation is not null)
        {
            return validation;
        }

        validation = await ValidateEmpresaDepartamentoCargoAsync(request.EmpresaTrasladoNuevaId, request.DepartamentoTrasladoNuevoId, request.CargoTrasladoNuevoId, "de traslado", cancellationToken);
        if (validation is not null)
        {
            return validation;
        }

        if (request.JefeNuevoId.HasValue)
        {
            validation = await ValidateJefeAsync(request.ColaboradorId, request.JefeNuevoId.Value, "Jefe nuevo", cancellationToken);
            if (validation is not null)
            {
                return validation;
            }
        }

        if (request.JefeTrasladoNuevoId.HasValue)
        {
            validation = await ValidateJefeAsync(request.ColaboradorId, request.JefeTrasladoNuevoId.Value, "Jefe de traslado", cancellationToken);
            if (validation is not null)
            {
                return validation;
            }
        }

        return null;
    }

    private async Task<string?> ValidateEmpresaDepartamentoCargoAsync(int? empresaId, int? departamentoId, int? cargoId, string label, CancellationToken cancellationToken)
    {
        if (empresaId.HasValue && !await db.Empresas.AnyAsync(x => x.EmpresaId == empresaId.Value && x.IsActive, cancellationToken))
        {
            return $"Empresa {label} no valida.";
        }

        if (departamentoId.HasValue)
        {
            var departamento = await db.Departamentos.AsNoTracking().FirstOrDefaultAsync(x => x.DepartamentoId == departamentoId.Value && x.IsActive, cancellationToken);
            if (departamento is null || empresaId.HasValue && departamento.EmpresaId != empresaId.Value)
            {
                return $"Departamento {label} no pertenece a la empresa seleccionada.";
            }
        }

        if (cargoId.HasValue)
        {
            var cargo = await db.Cargos.AsNoTracking().FirstOrDefaultAsync(x => x.CargoId == cargoId.Value && x.IsActive, cancellationToken);
            if (cargo is null || departamentoId.HasValue && cargo.DepartamentoId != departamentoId.Value)
            {
                return $"Cargo {label} no pertenece al departamento seleccionado.";
            }
        }

        return null;
    }

    private async Task<string?> ValidateJefeAsync(int? colaboradorId, int jefeId, string label, CancellationToken cancellationToken)
    {
        if (colaboradorId.HasValue && colaboradorId.Value == jefeId)
        {
            return $"{label} no puede ser el mismo colaborador.";
        }

        var jefe = await db.Colaboradores.AsNoTracking().FirstOrDefaultAsync(x => x.ColaboradorId == jefeId, cancellationToken);
        if (jefe is null)
        {
            return $"{label} no encontrado.";
        }

        return jefe.IsActive ? null : $"{label} inactivo.";
    }

    private static string? ValidateVacaciones(AccionPersonalRequestBase request)
    {
        if (!request.DiasVacaciones.HasValue || request.DiasVacaciones.Value <= 0)
        {
            return "Dias de vacaciones es obligatorio.";
        }

        if (!request.FechaInicioVacaciones.HasValue || !request.FechaFinVacaciones.HasValue)
        {
            return "Fechas de vacaciones son obligatorias.";
        }

        return request.FechaFinVacaciones.Value.Date < request.FechaInicioVacaciones.Value.Date
            ? "Fecha fin de vacaciones no puede ser anterior a fecha inicio."
            : null;
    }

    private static string? ValidateContratacion(AccionPersonalRequestBase request)
    {
        if (!request.TipoContratoNuevoId.HasValue)
        {
            return "Tipo de contrato nuevo es obligatorio.";
        }

        if (request.SalarioNuevo is null or < 0)
        {
            return "Salario nuevo es obligatorio.";
        }

        if (request.EsReemplazo is null && request.EsPosicionNueva is null)
        {
            return "Debe indicar si es reemplazo o posicion nueva.";
        }

        return request.EsReemplazo == true && request.EsPosicionNueva == true
            ? "La accion no puede ser reemplazo y posicion nueva al mismo tiempo."
            : null;
    }

    private static string? ValidateAjusteSalario(AccionPersonalRequestBase request)
    {
        if (request.SalarioNuevoAjuste is null or < 0)
        {
            return "Nuevo salario de ajuste es obligatorio.";
        }

        return string.IsNullOrWhiteSpace(request.MotivoAjuste)
            ? "Motivo de ajuste es obligatorio."
            : null;
    }

    private static string? ValidateCambioPosicion(AccionPersonalRequestBase request)
    {
        if (!request.CargoNuevoId.HasValue)
        {
            return "Cargo nuevo es obligatorio.";
        }

        return !request.DepartamentoNuevoId.HasValue ? "Departamento nuevo es obligatorio." : null;
    }

    private static string? ValidateTraslado(AccionPersonalRequestBase request)
    {
        return !request.DepartamentoTrasladoNuevoId.HasValue ? "Departamento de traslado nuevo es obligatorio." : null;
    }

    private static string? ValidateLicencia(AccionPersonalRequestBase request)
    {
        if (!request.FechaInicioLicencia.HasValue || !request.FechaFinLicencia.HasValue)
        {
            return "Fechas de licencia son obligatorias.";
        }

        if (request.FechaFinLicencia.Value.Date < request.FechaInicioLicencia.Value.Date)
        {
            return "Fecha fin de licencia no puede ser anterior a fecha inicio.";
        }

        if (!request.LicenciaRemunerada.HasValue)
        {
            return "Debe indicar si la licencia es remunerada.";
        }

        return string.IsNullOrWhiteSpace(request.EspecificacionLicencia)
            ? "Especificacion de licencia es obligatoria."
            : null;
    }

    private static string? ValidateFinalizacion(AccionPersonalRequestBase request)
    {
        if (!request.FechaSalida.HasValue)
        {
            return "Fecha de salida es obligatoria.";
        }

        if (!request.MotivoSalidaId.HasValue)
        {
            return "Motivo de salida es obligatorio.";
        }

        if (string.IsNullOrWhiteSpace(request.TipoFinalizacion))
        {
            return "Tipo de finalizacion es obligatorio.";
        }

        var evaluationValues = new[]
        {
            request.Puntualidad,
            request.Honestidad,
            request.TrabajoEquipo,
            request.Productividad,
            request.Iniciativa,
            request.RespetoJefe,
            request.RespetoCompaneros
        };

        return evaluationValues.Any(value => !string.IsNullOrWhiteSpace(value) && !EvaluationValues.Contains(value.Trim()))
            ? "La evaluacion de salida solo acepta Deficiente, Regular, Bueno o Excelente."
            : null;
    }

    private async Task<ApproverSelection> ResolveApproverAsync(RequisicionPersonalRequestBase request, CancellationToken cancellationToken)
    {
        return await ResolveApproverAsync(request.DepartamentoResponsableId, request.LiderAprobadorUsuarioId, request.LiderAprobadorColaboradorId, cancellationToken);
    }

    private async Task<ApproverSelection> ResolveApproverAsync(int? departamentoResponsableId, int? liderAprobadorUsuarioId, int? liderAprobadorColaboradorId, CancellationToken cancellationToken)
    {
        if (!departamentoResponsableId.HasValue)
        {
            return new ApproverSelection(liderAprobadorUsuarioId, liderAprobadorColaboradorId, null);
        }

        var responsable = await db.DepartamentoResponsables
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.DepartamentoResponsableId == departamentoResponsableId.Value, cancellationToken);

        return responsable is null
            ? new ApproverSelection(liderAprobadorUsuarioId, liderAprobadorColaboradorId, departamentoResponsableId)
            : new ApproverSelection(responsable.UsuarioResponsableId, responsable.ColaboradorResponsableId, responsable.DepartamentoResponsableId);
    }

    private Task<string> GenerateCodigoSolicitudAsync(CancellationToken cancellationToken)
        => GenerateCodigoSolicitudAsync(TipoSolicitud.RequisicionPersonal, cancellationToken);

    private async Task<string> GenerateCodigoSolicitudAsync(TipoSolicitud tipoSolicitud, CancellationToken cancellationToken)
    {
        var year = DateTime.UtcNow.Year;
        var code = tipoSolicitud == TipoSolicitud.AccionPersonal ? "AP" : "REQ";
        var prefix = $"{code}-{year}-";
        var count = await db.Solicitudes.CountAsync(x => x.CodigoSolicitud.StartsWith(prefix), cancellationToken);
        return $"{prefix}{count + 1:000000}";
    }

    private static SolicitudListDto ToListDto(Solicitud solicitud)
    {
        return new SolicitudListDto(
            solicitud.SolicitudId,
            solicitud.CodigoSolicitud,
            solicitud.TipoSolicitud.ToString(),
            solicitud.Estado.ToString(),
            solicitud.SolicitanteUsuario.NombreUsuario,
            solicitud.Empresa?.Nombre,
            solicitud.Departamento?.Nombre,
            solicitud.FechaSolicitud,
            solicitud.UpdatedAt);
    }

    private SolicitudDetailDto ToDetailDto(Solicitud solicitud)
    {
        return new SolicitudDetailDto(
            solicitud.SolicitudId,
            solicitud.CodigoSolicitud,
            solicitud.TipoSolicitud.ToString(),
            solicitud.Estado.ToString(),
            solicitud.SolicitanteUsuarioId,
            solicitud.SolicitanteUsuario.NombreUsuario,
            solicitud.ColaboradorId,
            solicitud.EmpresaId,
            solicitud.Empresa?.Nombre,
            solicitud.DepartamentoId,
            solicitud.Departamento?.Nombre,
            solicitud.CargoId,
            solicitud.Cargo?.Nombre,
            solicitud.FechaSolicitud,
            solicitud.FechaEfectiva,
            solicitud.Justificacion,
            solicitud.Observaciones,
            solicitud.CreatedAt,
            solicitud.UpdatedAt,
            solicitud.RequisicionPersonal is null ? null : ToRequisicionDto(solicitud.RequisicionPersonal),
            solicitud.AccionPersonal is null ? null : ToAccionPersonalDto(solicitud.AccionPersonal),
            solicitud.Aprobaciones.OrderBy(x => x.Orden).Select(ToAprobacionDto).ToList(),
            solicitud.Historial.OrderByDescending(x => x.Fecha).Select(ToHistorialDto).ToList(),
            GetAvailableActions(solicitud));
    }

    private static RequisicionPersonalDto ToRequisicionDto(RequisicionPersonal requisicion)
    {
        return new RequisicionPersonalDto(
            requisicion.RequisicionPersonalId,
            requisicion.SolicitudId,
            requisicion.CargoSolicitado,
            requisicion.DepartamentoSolicitadoId,
            requisicion.DepartamentoSolicitado?.Nombre,
            requisicion.NumeroPlazas,
            requisicion.DependenciaJerarquica,
            requisicion.PrincipalesResponsabilidades,
            requisicion.FuncionesEspecificas,
            requisicion.EquipoACargo,
            requisicion.CentroTrabajo,
            requisicion.Salario,
            requisicion.GastoRepresentacion,
            requisicion.SalarioVariable,
            requisicion.OtrosConceptos,
            requisicion.EsPosicionNueva,
            requisicion.EsReemplazo,
            requisicion.ColaboradorReemplazadoId,
            requisicion.ColaboradorReemplazado?.NombreCompleto(),
            requisicion.NombrePersonaReemplazada,
            requisicion.TipoContratoId,
            requisicion.TipoContrato?.Nombre,
            requisicion.PeriodoPrueba,
            requisicion.FormacionRequerida,
            requisicion.FormacionComplementaria,
            requisicion.ConocimientosTecnicos,
            requisicion.ConocimientosValorados,
            requisicion.IdiomaNivel,
            requisicion.AniosExperiencia,
            requisicion.FuncionesExperiencia,
            requisicion.AreaSectorExperiencia,
            requisicion.ExperienciaValorable,
            requisicion.EdadMinima,
            requisicion.EdadMaxima,
            requisicion.SexoPreferido,
            requisicion.CaracteristicasPersonales,
            requisicion.FechaAperturaProceso,
            requisicion.FechaEntregaCandidatos,
            requisicion.SolicitadoPorTexto,
            requisicion.AutorizadoPorTexto,
            requisicion.FechaAutorizacion);
    }

    private static AccionPersonalDto ToAccionPersonalDto(AccionPersonal accion)
    {
        return new AccionPersonalDto(
            accion.AccionPersonalId,
            accion.SolicitudId,
            accion.TipoAccion.ToString(),
            FormatTipoAccion(accion.TipoAccion),
            accion.ColaboradorId,
            accion.NombreColaboradorSnapshot ?? accion.Colaborador?.NombreCompleto(),
            accion.NoEmpleadoSnapshot,
            accion.CedulaSnapshot,
            accion.FechaEfectiva,
            accion.Justificacion,
            accion.Observaciones,
            accion.EmpresaActualId,
            accion.EmpresaActual?.Nombre,
            accion.DepartamentoActualId,
            accion.DepartamentoActual?.Nombre,
            accion.CargoActualId,
            accion.CargoActual?.Nombre,
            accion.JefeActualId,
            accion.JefeActual?.NombreCompleto(),
            accion.TipoContratoActualId,
            accion.TipoContratoActual?.Nombre,
            accion.EstatusActualId,
            accion.EstatusActual?.Nombre,
            accion.SalarioActual,
            accion.ViaticosActual,
            accion.GastosRepresentacionActual,
            accion.DiasVacaciones,
            accion.FechaInicioVacaciones,
            accion.FechaFinVacaciones,
            accion.PeriodoVacacionesDesde,
            accion.PeriodoVacacionesHasta,
            accion.QuienReemplaza,
            accion.TipoContratoNuevoId,
            accion.TipoContratoNuevo?.Nombre,
            accion.FechaInicioContrato,
            accion.FechaFinContrato,
            accion.EsReemplazo,
            accion.EsPosicionNueva,
            accion.SalarioNuevo,
            accion.ViaticosNuevo,
            accion.GastosRepresentacionNuevo,
            accion.OtrosBeneficios,
            accion.SalarioAnterior,
            accion.SalarioNuevoAjuste,
            accion.AjustePorMes,
            accion.MotivoAjuste,
            accion.CargoNuevoId,
            accion.CargoNuevo?.Nombre,
            accion.DepartamentoNuevoId,
            accion.DepartamentoNuevo?.Nombre,
            accion.EmpresaNuevaId,
            accion.EmpresaNueva?.Nombre,
            accion.JefeNuevoId,
            accion.JefeNuevo?.NombreCompleto(),
            accion.CargoTrasladoActualId,
            accion.CargoTrasladoActual?.Nombre,
            accion.CargoTrasladoNuevoId,
            accion.CargoTrasladoNuevo?.Nombre,
            accion.DepartamentoTrasladoActualId,
            accion.DepartamentoTrasladoActual?.Nombre,
            accion.DepartamentoTrasladoNuevoId,
            accion.DepartamentoTrasladoNuevo?.Nombre,
            accion.EmpresaTrasladoActualId,
            accion.EmpresaTrasladoActual?.Nombre,
            accion.EmpresaTrasladoNuevaId,
            accion.EmpresaTrasladoNueva?.Nombre,
            accion.JefeTrasladoNuevoId,
            accion.JefeTrasladoNuevo?.NombreCompleto(),
            accion.TipoLicenciaAccion,
            accion.LicenciaRemunerada,
            accion.FechaInicioLicencia,
            accion.FechaFinLicencia,
            accion.EspecificacionLicencia,
            accion.TipoFinalizacion,
            accion.FechaSalida,
            accion.MotivoSalidaId,
            accion.MotivoSalida?.Nombre,
            accion.MenosDeDosAnios,
            accion.TerminacionPeriodoPrueba,
            accion.CausaJustificada,
            accion.MutuoAcuerdo,
            accion.RenovacionExtensionContrato,
            accion.ContinuidadLaboral,
            accion.LoRecomienda,
            accion.Puntualidad,
            accion.Honestidad,
            accion.TrabajoEquipo,
            accion.Productividad,
            accion.Iniciativa,
            accion.RespetoJefe,
            accion.RespetoCompaneros,
            accion.Ejecutada,
            accion.FechaEjecucion,
            accion.EjecutadaPorUsuarioId,
            accion.EjecutadaPorUsuario?.NombreUsuario,
            accion.ResultadoEjecucion,
            accion.ErrorEjecucion,
            accion.CambiosAplicados.OrderByDescending(x => x.Fecha).Select(ToCambioAplicadoDto).ToList());
    }

    private static AccionPersonalCambioAplicadoDto ToCambioAplicadoDto(AccionPersonalCambioAplicado cambio)
    {
        return new AccionPersonalCambioAplicadoDto(
            cambio.AccionPersonalCambioAplicadoId,
            cambio.Campo,
            cambio.ValorAnterior,
            cambio.ValorNuevo,
            cambio.Fecha,
            cambio.UsuarioId,
            cambio.Usuario?.NombreUsuario ?? "N/D");
    }

    private static SolicitudAprobacionDto ToAprobacionDto(SolicitudAprobacion aprobacion)
    {
        return new SolicitudAprobacionDto(
            aprobacion.SolicitudAprobacionId,
            aprobacion.Orden,
            aprobacion.Etapa.ToString(),
            aprobacion.RolAprobador,
            aprobacion.UsuarioAprobadorId,
            aprobacion.UsuarioAprobador?.NombreUsuario,
            aprobacion.ColaboradorAprobadorId,
            aprobacion.ColaboradorAprobador?.NombreCompleto(),
            aprobacion.DepartamentoResponsableId,
            aprobacion.DepartamentoResponsable?.TipoResponsable,
            aprobacion.Estado.ToString(),
            aprobacion.FechaDecision,
            aprobacion.Comentario);
    }

    private static SolicitudHistorialDto ToHistorialDto(SolicitudHistorial historial)
    {
        return new SolicitudHistorialDto(
            historial.SolicitudHistorialId,
            historial.Accion,
            historial.EstadoAnterior?.ToString(),
            historial.EstadoNuevo?.ToString(),
            historial.Comentario,
            historial.UsuarioId,
            historial.Usuario?.NombreUsuario ?? "N/D",
            historial.Fecha);
    }

    private static RequisicionPersonal BuildRequisicion(RequisicionPersonalRequestBase request)
    {
        var requisicion = new RequisicionPersonal();
        ApplyRequisicion(requisicion, request);
        return requisicion;
    }

    private static void ApplySolicitud(Solicitud solicitud, RequisicionPersonalRequestBase request)
    {
        solicitud.EmpresaId = request.EmpresaId;
        solicitud.DepartamentoId = request.DepartamentoSolicitadoId;
        solicitud.FechaEfectiva = request.FechaEfectiva;
        solicitud.Justificacion = request.Justificacion?.Trim();
        solicitud.Observaciones = request.Observaciones?.Trim();
    }

    private static void ApplyRequisicion(RequisicionPersonal requisicion, RequisicionPersonalRequestBase request)
    {
        requisicion.CargoSolicitado = request.CargoSolicitado.Trim();
        requisicion.DepartamentoSolicitadoId = request.DepartamentoSolicitadoId;
        requisicion.NumeroPlazas = request.NumeroPlazas;
        requisicion.DependenciaJerarquica = request.DependenciaJerarquica?.Trim();
        requisicion.PrincipalesResponsabilidades = request.PrincipalesResponsabilidades?.Trim();
        requisicion.FuncionesEspecificas = request.FuncionesEspecificas?.Trim();
        requisicion.EquipoACargo = request.EquipoACargo?.Trim();
        requisicion.CentroTrabajo = request.CentroTrabajo?.Trim();
        requisicion.Salario = request.Salario;
        requisicion.GastoRepresentacion = request.GastoRepresentacion;
        requisicion.SalarioVariable = request.SalarioVariable;
        requisicion.OtrosConceptos = request.OtrosConceptos?.Trim();
        requisicion.EsPosicionNueva = request.EsPosicionNueva;
        requisicion.EsReemplazo = request.EsReemplazo;
        requisicion.ColaboradorReemplazadoId = request.ColaboradorReemplazadoId;
        requisicion.NombrePersonaReemplazada = request.NombrePersonaReemplazada?.Trim();
        requisicion.TipoContratoId = request.TipoContratoId;
        requisicion.PeriodoPrueba = request.PeriodoPrueba?.Trim();
        requisicion.FormacionRequerida = request.FormacionRequerida?.Trim();
        requisicion.FormacionComplementaria = request.FormacionComplementaria?.Trim();
        requisicion.ConocimientosTecnicos = request.ConocimientosTecnicos?.Trim();
        requisicion.ConocimientosValorados = request.ConocimientosValorados?.Trim();
        requisicion.IdiomaNivel = request.IdiomaNivel?.Trim();
        requisicion.AniosExperiencia = request.AniosExperiencia;
        requisicion.FuncionesExperiencia = request.FuncionesExperiencia?.Trim();
        requisicion.AreaSectorExperiencia = request.AreaSectorExperiencia?.Trim();
        requisicion.ExperienciaValorable = request.ExperienciaValorable?.Trim();
        requisicion.EdadMinima = request.EdadMinima;
        requisicion.EdadMaxima = request.EdadMaxima;
        requisicion.SexoPreferido = request.SexoPreferido?.Trim();
        requisicion.CaracteristicasPersonales = request.CaracteristicasPersonales?.Trim();
        requisicion.FechaAperturaProceso = request.FechaAperturaProceso;
        requisicion.FechaEntregaCandidatos = request.FechaEntregaCandidatos;
    }

    private async Task<AccionPersonal> BuildAccionPersonalAsync(AccionPersonalRequestBase request, TipoAccionPersonal tipoAccion, CancellationToken cancellationToken)
    {
        var accion = new AccionPersonal();
        await ApplyAccionPersonalAsync(accion, request, tipoAccion, cancellationToken);
        return accion;
    }

    private async Task ApplyAccionPersonalAsync(AccionPersonal accion, AccionPersonalRequestBase request, TipoAccionPersonal tipoAccion, CancellationToken cancellationToken)
    {
        var colaborador = request.ColaboradorId.HasValue
            ? await db.Colaboradores
                .Include(x => x.Empresa)
                .Include(x => x.Departamento)
                .Include(x => x.Cargo)
                .Include(x => x.JefeInmediato)
                .Include(x => x.TipoContrato)
                .Include(x => x.Estatus)
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.ColaboradorId == request.ColaboradorId.Value, cancellationToken)
            : null;

        accion.TipoAccion = tipoAccion;
        accion.ColaboradorId = request.ColaboradorId;
        accion.FechaEfectiva = request.FechaEfectiva!.Value.Date;
        accion.Justificacion = request.Justificacion!.Trim();
        accion.Observaciones = request.Observaciones?.Trim();

        accion.NombreColaboradorSnapshot = colaborador?.NombreCompleto();
        accion.NoEmpleadoSnapshot = colaborador?.NoEmpleado;
        accion.CedulaSnapshot = colaborador?.Cedula;
        accion.EmpresaActualId = colaborador?.EmpresaId;
        accion.DepartamentoActualId = colaborador?.DepartamentoId;
        accion.CargoActualId = colaborador?.CargoId;
        accion.JefeActualId = colaborador?.JefeInmediatoId;
        accion.TipoContratoActualId = colaborador?.TipoContratoId;
        accion.EstatusActualId = colaborador?.EstatusId;
        accion.SalarioActual = colaborador?.Salario;
        accion.ViaticosActual = colaborador?.Viaticos;
        accion.GastosRepresentacionActual = colaborador?.GastosRepresentacion;

        accion.DiasVacaciones = request.DiasVacaciones;
        accion.FechaInicioVacaciones = request.FechaInicioVacaciones;
        accion.FechaFinVacaciones = request.FechaFinVacaciones;
        accion.PeriodoVacacionesDesde = request.PeriodoVacacionesDesde;
        accion.PeriodoVacacionesHasta = request.PeriodoVacacionesHasta;
        accion.QuienReemplaza = request.QuienReemplaza?.Trim();

        accion.TipoContratoNuevoId = request.TipoContratoNuevoId;
        accion.FechaInicioContrato = request.FechaInicioContrato;
        accion.FechaFinContrato = request.FechaFinContrato;
        accion.EsReemplazo = request.EsReemplazo;
        accion.EsPosicionNueva = request.EsPosicionNueva;
        accion.SalarioNuevo = request.SalarioNuevo;
        accion.ViaticosNuevo = request.ViaticosNuevo;
        accion.GastosRepresentacionNuevo = request.GastosRepresentacionNuevo;
        accion.OtrosBeneficios = request.OtrosBeneficios?.Trim();

        accion.SalarioAnterior = colaborador?.Salario;
        accion.SalarioNuevoAjuste = request.SalarioNuevoAjuste;
        accion.AjustePorMes = request.AjustePorMes;
        accion.MotivoAjuste = request.MotivoAjuste?.Trim();

        accion.CargoNuevoId = request.CargoNuevoId;
        accion.DepartamentoNuevoId = request.DepartamentoNuevoId;
        accion.EmpresaNuevaId = request.EmpresaNuevaId;
        accion.JefeNuevoId = request.JefeNuevoId;

        accion.CargoTrasladoActualId = colaborador?.CargoId;
        accion.CargoTrasladoNuevoId = request.CargoTrasladoNuevoId;
        accion.DepartamentoTrasladoActualId = colaborador?.DepartamentoId;
        accion.DepartamentoTrasladoNuevoId = request.DepartamentoTrasladoNuevoId;
        accion.EmpresaTrasladoActualId = colaborador?.EmpresaId;
        accion.EmpresaTrasladoNuevaId = request.EmpresaTrasladoNuevaId;
        accion.JefeTrasladoNuevoId = request.JefeTrasladoNuevoId;

        accion.TipoLicenciaAccion = request.TipoLicenciaAccion?.Trim();
        accion.LicenciaRemunerada = request.LicenciaRemunerada;
        accion.FechaInicioLicencia = request.FechaInicioLicencia;
        accion.FechaFinLicencia = request.FechaFinLicencia;
        accion.EspecificacionLicencia = request.EspecificacionLicencia?.Trim();

        accion.TipoFinalizacion = request.TipoFinalizacion?.Trim();
        accion.FechaSalida = request.FechaSalida;
        accion.MotivoSalidaId = request.MotivoSalidaId;
        accion.MenosDeDosAnios = request.MenosDeDosAnios;
        accion.TerminacionPeriodoPrueba = request.TerminacionPeriodoPrueba;
        accion.CausaJustificada = request.CausaJustificada;
        accion.MutuoAcuerdo = request.MutuoAcuerdo;
        accion.RenovacionExtensionContrato = request.RenovacionExtensionContrato;
        accion.ContinuidadLaboral = request.ContinuidadLaboral;
        accion.LoRecomienda = request.LoRecomienda;

        accion.Puntualidad = NormalizeEvaluation(request.Puntualidad);
        accion.Honestidad = NormalizeEvaluation(request.Honestidad);
        accion.TrabajoEquipo = NormalizeEvaluation(request.TrabajoEquipo);
        accion.Productividad = NormalizeEvaluation(request.Productividad);
        accion.Iniciativa = NormalizeEvaluation(request.Iniciativa);
        accion.RespetoJefe = NormalizeEvaluation(request.RespetoJefe);
        accion.RespetoCompaneros = NormalizeEvaluation(request.RespetoCompaneros);

        accion.Ejecutada = false;
        accion.FechaEjecucion = null;
        accion.EjecutadaPorUsuarioId = null;
        accion.ResultadoEjecucion = null;
        accion.ErrorEjecucion = null;
    }

    private static void ApplySolicitud(Solicitud solicitud, AccionPersonalRequestBase request, AccionPersonal accion)
    {
        solicitud.ColaboradorId = accion.ColaboradorId;
        solicitud.EmpresaId = request.EmpresaId ?? accion.EmpresaActualId ?? accion.EmpresaNuevaId ?? accion.EmpresaTrasladoNuevaId;
        solicitud.DepartamentoId = request.DepartamentoId ?? accion.DepartamentoActualId ?? accion.DepartamentoNuevoId ?? accion.DepartamentoTrasladoNuevoId;
        solicitud.CargoId = request.CargoId ?? accion.CargoActualId ?? accion.CargoNuevoId ?? accion.CargoTrasladoNuevoId;
        solicitud.FechaEfectiva = accion.FechaEfectiva;
        solicitud.Justificacion = accion.Justificacion;
        solicitud.Observaciones = accion.Observaciones;
    }

    private async Task<ExecutionResult> ExecuteAccionPersonalAsync(Solicitud solicitud, string? comentario, CancellationToken cancellationToken)
    {
        var accion = solicitud.AccionPersonal!;
        accion.FechaEjecucion = DateTime.UtcNow;
        accion.EjecutadaPorUsuarioId = User.CurrentUserId();
        accion.ErrorEjecucion = null;

        if (!accion.ColaboradorId.HasValue)
        {
            accion.Ejecutada = true;
            accion.ResultadoEjecucion = "Ejecucion registrada sin cambios automaticos en Colaboradores.";
            return ExecutionResult.Ok(accion.ResultadoEjecucion);
        }

        var colaborador = await db.Colaboradores.FirstOrDefaultAsync(x => x.ColaboradorId == accion.ColaboradorId.Value, cancellationToken);
        if (colaborador is null)
        {
            accion.ErrorEjecucion = "Colaborador relacionado no encontrado al ejecutar.";
            return ExecutionResult.Fail(accion.ErrorEjecucion);
        }

        var appliedChanges = 0;
        switch (accion.TipoAccion)
        {
            case TipoAccionPersonal.AjusteSalario:
                appliedChanges += ApplyColaboradorChange(accion, colaborador, "Salario", colaborador.Salario, accion.SalarioNuevoAjuste, value => colaborador.Salario = value);
                break;

            case TipoAccionPersonal.CambioPosicion:
                appliedChanges += ApplyColaboradorChange(accion, colaborador, "EmpresaId", colaborador.EmpresaId, accion.EmpresaNuevaId, value => colaborador.EmpresaId = value);
                appliedChanges += ApplyColaboradorChange(accion, colaborador, "DepartamentoId", colaborador.DepartamentoId, accion.DepartamentoNuevoId, value => colaborador.DepartamentoId = value);
                appliedChanges += ApplyColaboradorChange(accion, colaborador, "CargoId", colaborador.CargoId, accion.CargoNuevoId, value => colaborador.CargoId = value);
                appliedChanges += ApplyColaboradorChange(accion, colaborador, "JefeInmediatoId", colaborador.JefeInmediatoId, accion.JefeNuevoId, value => colaborador.JefeInmediatoId = value);
                break;

            case TipoAccionPersonal.TrasladoCambioArea:
                appliedChanges += ApplyColaboradorChange(accion, colaborador, "EmpresaId", colaborador.EmpresaId, accion.EmpresaTrasladoNuevaId, value => colaborador.EmpresaId = value);
                appliedChanges += ApplyColaboradorChange(accion, colaborador, "DepartamentoId", colaborador.DepartamentoId, accion.DepartamentoTrasladoNuevoId, value => colaborador.DepartamentoId = value);
                appliedChanges += ApplyColaboradorChange(accion, colaborador, "CargoId", colaborador.CargoId, accion.CargoTrasladoNuevoId, value => colaborador.CargoId = value);
                appliedChanges += ApplyColaboradorChange(accion, colaborador, "JefeInmediatoId", colaborador.JefeInmediatoId, accion.JefeTrasladoNuevoId, value => colaborador.JefeInmediatoId = value);
                break;

            case TipoAccionPersonal.FinalizacionDesvinculacion:
                var cesanteId = await db.EstatusColaborador
                    .Where(x => x.Codigo == "C" && x.IsActive)
                    .Select(x => (int?)x.EstatusId)
                    .FirstOrDefaultAsync(cancellationToken);

                if (!cesanteId.HasValue)
                {
                    accion.ErrorEjecucion = "No existe estatus Cesante activo para ejecutar finalizacion.";
                    return ExecutionResult.Fail(accion.ErrorEjecucion);
                }

                appliedChanges += ApplyColaboradorChange(accion, colaborador, "EstatusId", colaborador.EstatusId, cesanteId, value => colaborador.EstatusId = value);
                appliedChanges += ApplyColaboradorChange(accion, colaborador, "FechaSalida", colaborador.FechaSalida, accion.FechaSalida, value => colaborador.FechaSalida = value);
                appliedChanges += ApplyColaboradorChange(accion, colaborador, "MotivoSalidaId", colaborador.MotivoSalidaId, accion.MotivoSalidaId, value => colaborador.MotivoSalidaId = value);
                break;

            default:
                AddExecutionHistoryOnly(accion, colaborador, comentario);
                break;
        }

        if (appliedChanges > 0)
        {
            colaborador.UpdatedBy = User.Identity?.Name;
            accion.ResultadoEjecucion = $"Accion ejecutada. Cambios aplicados: {appliedChanges}.";
        }
        else if (accion.ResultadoEjecucion is null)
        {
            accion.ResultadoEjecucion = "Accion ejecutada sin cambios automaticos en Colaboradores.";
        }

        accion.Ejecutada = true;
        return ExecutionResult.Ok(accion.ResultadoEjecucion);
    }

    private int ApplyColaboradorChange<T>(AccionPersonal accion, Colaborador colaborador, string campo, T currentValue, T? newValue, Action<T> apply)
        where T : struct
    {
        if (!newValue.HasValue || EqualityComparer<T>.Default.Equals(currentValue, newValue.Value))
        {
            return 0;
        }

        var oldText = FormatValue(currentValue);
        var newText = FormatValue(newValue.Value);
        apply(newValue.Value);
        AddAppliedChange(accion, colaborador, campo, oldText, newText);
        return 1;
    }

    private int ApplyColaboradorChange<T>(AccionPersonal accion, Colaborador colaborador, string campo, T? currentValue, T? newValue, Action<T?> apply)
        where T : struct
    {
        if (!newValue.HasValue || EqualityComparer<T?>.Default.Equals(currentValue, newValue))
        {
            return 0;
        }

        var oldText = FormatValue(currentValue);
        var newText = FormatValue(newValue);
        apply(newValue);
        AddAppliedChange(accion, colaborador, campo, oldText, newText);
        return 1;
    }

    private void AddAppliedChange(AccionPersonal accion, Colaborador colaborador, string campo, string? valorAnterior, string? valorNuevo)
    {
        accion.CambiosAplicados.Add(new AccionPersonalCambioAplicado
        {
            Campo = campo,
            ValorAnterior = valorAnterior,
            ValorNuevo = valorNuevo,
            Fecha = DateTime.UtcNow,
            UsuarioId = User.CurrentUserId(),
            CreatedBy = User.Identity?.Name
        });

        db.HistorialColaborador.Add(new HistorialColaborador
        {
            ColaboradorId = colaborador.ColaboradorId,
            UsuarioId = User.CurrentUserId(),
            Accion = "ACCION_PERSONAL_EJECUTADA",
            Campo = campo,
            ValorAnterior = valorAnterior,
            ValorNuevo = valorNuevo,
            Observacion = $"Solicitud {accion.Solicitud?.CodigoSolicitud ?? accion.SolicitudId.ToString(CultureInfo.InvariantCulture)} - {FormatTipoAccion(accion.TipoAccion)}",
            CreatedBy = User.Identity?.Name
        });
    }

    private void AddExecutionHistoryOnly(AccionPersonal accion, Colaborador colaborador, string? comentario)
    {
        db.HistorialColaborador.Add(new HistorialColaborador
        {
            ColaboradorId = colaborador.ColaboradorId,
            UsuarioId = User.CurrentUserId(),
            Accion = "ACCION_PERSONAL_EJECUTADA",
            Campo = "SinCambioAutomatico",
            ValorNuevo = FormatTipoAccion(accion.TipoAccion),
            Observacion = string.IsNullOrWhiteSpace(comentario)
                ? "Ejecucion controlada sin modificacion automatica de colaborador."
                : comentario.Trim(),
            CreatedBy = User.Identity?.Name
        });
    }

    private void SetApprovalFlow(Solicitud solicitud, ApproverSelection approver, EstadoSolicitud state)
    {
        var leader = GetOrCreateApproval(solicitud, 1, EtapaAprobacion.Lider, AppRoles.Supervisor);
        leader.UsuarioAprobadorId = approver.UsuarioId;
        leader.ColaboradorAprobadorId = approver.ColaboradorId;
        leader.DepartamentoResponsableId = approver.DepartamentoResponsableId;
        leader.Estado = approver.HasLeader
            ? state == EstadoSolicitud.PendienteAprobacionLider ? EstadoAprobacion.Pendiente : EstadoAprobacion.Omitida
            : EstadoAprobacion.Omitida;
        leader.FechaDecision = null;
        leader.Comentario = null;

        var rrhh = GetOrCreateApproval(solicitud, 2, EtapaAprobacion.RRHH, AppRoles.RRHH);
        rrhh.ColaboradorAprobadorId = null;
        rrhh.DepartamentoResponsableId = null;
        rrhh.Estado = state == EstadoSolicitud.PendienteRevisionRRHH ? EstadoAprobacion.Pendiente : EstadoAprobacion.Omitida;
        rrhh.FechaDecision = null;
        rrhh.Comentario = null;
    }

    private static SolicitudAprobacion GetOrCreateApproval(Solicitud solicitud, int orden, EtapaAprobacion etapa, string rol)
    {
        var approval = solicitud.Aprobaciones.FirstOrDefault(x => x.Orden == orden);
        if (approval is not null)
        {
            approval.Etapa = etapa;
            approval.RolAprobador = rol;
            return approval;
        }

        approval = new SolicitudAprobacion
        {
            Orden = orden,
            Etapa = etapa,
            RolAprobador = rol
        };
        solicitud.Aprobaciones.Add(approval);
        return approval;
    }

    private static EstadoSolicitud GetSubmittedState(ApproverSelection approver)
    {
        return approver.HasLeader ? EstadoSolicitud.PendienteAprobacionLider : EstadoSolicitud.PendienteRevisionRRHH;
    }

    private static ApproverSelection ToApproverSelection(SolicitudAprobacion? aprobacion)
    {
        return aprobacion is null
            ? new ApproverSelection(null, null, null)
            : new ApproverSelection(aprobacion.UsuarioAprobadorId, aprobacion.ColaboradorAprobadorId, aprobacion.DepartamentoResponsableId);
    }

    private static SolicitudAprobacion? GetLeaderApproval(Solicitud solicitud)
    {
        return solicitud.Aprobaciones.FirstOrDefault(x => x.Etapa == EtapaAprobacion.Lider);
    }

    private static SolicitudAprobacion? GetRRHHApproval(Solicitud solicitud)
    {
        return solicitud.Aprobaciones.FirstOrDefault(x => x.Etapa == EtapaAprobacion.RRHH);
    }

    private static SolicitudAprobacion? GetPendingApprovalForCurrentStage(Solicitud solicitud)
    {
        return solicitud.Estado switch
        {
            EstadoSolicitud.PendienteAprobacionLider => GetLeaderApproval(solicitud),
            EstadoSolicitud.PendienteRevisionRRHH => GetRRHHApproval(solicitud),
            _ => null
        };
    }

    private void AddHistorial(Solicitud solicitud, string accion, EstadoSolicitud? anterior, EstadoSolicitud? nuevo, string? comentario)
    {
        solicitud.Historial.Add(new SolicitudHistorial
        {
            Accion = accion,
            EstadoAnterior = anterior,
            EstadoNuevo = nuevo,
            Comentario = comentario?.Trim(),
            UsuarioId = User.CurrentUserId(),
            Fecha = DateTime.UtcNow
        });
    }

    private static bool HasRequestedApprover(RequisicionPersonalRequestBase request)
    {
        return HasRequestedApprover(request.DepartamentoResponsableId, request.LiderAprobadorUsuarioId, request.LiderAprobadorColaboradorId);
    }

    private static bool HasRequestedApprover(int? departamentoResponsableId, int? liderAprobadorUsuarioId, int? liderAprobadorColaboradorId)
    {
        return departamentoResponsableId.HasValue ||
            liderAprobadorColaboradorId.HasValue ||
            liderAprobadorUsuarioId.HasValue;
    }

    private static bool IsOperationalCollaborator(Colaborador colaborador)
    {
        return colaborador.IsActive && OperationalStatusCodes.Contains(colaborador.Estatus.Codigo);
    }

    private static bool TryParseTipoAccion(string? value, out TipoAccionPersonal tipoAccion)
    {
        if (Enum.TryParse(value, true, out tipoAccion))
        {
            return true;
        }

        var normalized = NormalizeKey(value);
        foreach (var item in Enum.GetValues<TipoAccionPersonal>())
        {
            if (NormalizeKey(item.ToString()) == normalized || NormalizeKey(FormatTipoAccion(item)) == normalized)
            {
                tipoAccion = item;
                return true;
            }
        }

        return false;
    }

    private static string FormatTipoAccion(TipoAccionPersonal tipoAccion)
    {
        return tipoAccion switch
        {
            TipoAccionPersonal.Vacaciones => "Vacaciones",
            TipoAccionPersonal.ContratacionIngreso => "Contratacion / Ingreso",
            TipoAccionPersonal.AjusteSalario => "Ajuste de salario",
            TipoAccionPersonal.CambioPosicion => "Cambio de posicion",
            TipoAccionPersonal.TrasladoCambioArea => "Traslado / Cambio de area",
            TipoAccionPersonal.Licencia => "Licencia",
            TipoAccionPersonal.FinalizacionDesvinculacion => "Finalizacion / Desvinculacion",
            TipoAccionPersonal.RenovacionExtensionContrato => "Renovacion o extension de contrato",
            TipoAccionPersonal.ContinuidadLaboral => "Continuidad laboral",
            TipoAccionPersonal.Otro => "Otro",
            _ => tipoAccion.ToString()
        };
    }

    private static string NormalizeKey(string? value)
    {
        return string.Concat((value ?? string.Empty).Where(char.IsLetterOrDigit)).ToUpperInvariant();
    }

    private static string? NormalizeEvaluation(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var trimmed = value.Trim();
        return EvaluationValues.FirstOrDefault(x => string.Equals(x, trimmed, StringComparison.OrdinalIgnoreCase)) ?? trimmed;
    }

    private static string? FormatValue<T>(T value)
    {
        return value switch
        {
            null => null,
            DateTime date => date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            decimal amount => amount.ToString("0.##", CultureInfo.InvariantCulture),
            IFormattable formattable => formattable.ToString(null, CultureInfo.InvariantCulture),
            _ => value.ToString()
        };
    }

    private string CurrentUserName()
    {
        return User.Identity?.Name ?? $"Usuario {User.CurrentUserId()}";
    }

    private void MarkApproval(SolicitudAprobacion approval, EstadoAprobacion estado, string? comentario)
    {
        approval.Estado = estado;
        approval.FechaDecision = DateTime.UtcNow;
        approval.Comentario = comentario?.Trim();
        approval.UsuarioAprobadorId ??= User.CurrentUserId();
    }

    private bool CanView(Solicitud solicitud)
    {
        if (CanManageAll())
        {
            return true;
        }

        var userId = User.CurrentUserId();
        return solicitud.SolicitanteUsuarioId == userId || solicitud.Aprobaciones.Any(x => x.UsuarioAprobadorId == userId);
    }

    private bool CanEdit(Solicitud solicitud)
    {
        return CanManageAll() || solicitud.SolicitanteUsuarioId == User.CurrentUserId();
    }

    private bool CanApproveLeader(Solicitud solicitud)
    {
        if (CanManageAll())
        {
            return true;
        }

        var currentUserId = User.CurrentUserId();
        return User.IsInRole(AppRoles.Supervisor) && GetLeaderApproval(solicitud)?.UsuarioAprobadorId == currentUserId;
    }

    private bool CanReviewRRHH()
    {
        return User.IsInRole(AppRoles.Admin) || User.IsInRole(AppRoles.RRHH);
    }

    private bool CanDecideCurrentStage(Solicitud solicitud)
    {
        return solicitud.Estado switch
        {
            EstadoSolicitud.PendienteAprobacionLider => CanApproveLeader(solicitud),
            EstadoSolicitud.PendienteRevisionRRHH => CanReviewRRHH(),
            _ => false
        };
    }

    private bool CanManageAll()
    {
        return User.IsInRole(AppRoles.Admin) || User.IsInRole(AppRoles.RRHH);
    }

    private IReadOnlyList<string> GetAvailableActions(Solicitud solicitud)
    {
        var actions = new List<string>();

        if (EditableStates.Contains(solicitud.Estado) && CanEdit(solicitud))
        {
            actions.Add("editar");
            actions.Add("enviar");
        }

        if (!TerminalStates.Contains(solicitud.Estado) && CanEdit(solicitud))
        {
            actions.Add("cancelar");
        }

        if (solicitud.Estado == EstadoSolicitud.PendienteAprobacionLider && CanApproveLeader(solicitud))
        {
            actions.Add("aprobar");
            actions.Add("rechazar");
            actions.Add("devolver");
        }

        if (solicitud.Estado == EstadoSolicitud.PendienteRevisionRRHH && CanReviewRRHH())
        {
            actions.Add("confirmar-rrhh");
            actions.Add("rechazar");
            actions.Add("devolver");
        }

        if (solicitud.Estado == EstadoSolicitud.Aprobada && CanReviewRRHH())
        {
            if (solicitud.TipoSolicitud == TipoSolicitud.AccionPersonal && solicitud.AccionPersonal?.Ejecutada != true)
            {
                actions.Add("ejecutar-accion");
            }

            actions.Add("cerrar");
        }

        return actions;
    }
}
