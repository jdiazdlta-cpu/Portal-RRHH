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
        [EstadoSolicitud.Aprobada, EstadoSolicitud.Rechazada, EstadoSolicitud.Cancelada, EstadoSolicitud.Cerrada];
    private static readonly string[] OperationalStatusCodes = ["A", "V", "S"];

    private sealed record ApproverSelection(int? UsuarioId, int? ColaboradorId, int? DepartamentoResponsableId)
    {
        public bool HasLeader => UsuarioId.HasValue || ColaboradorId.HasValue || DepartamentoResponsableId.HasValue;
    }

    [HttpGet("tipos")]
    public IActionResult Tipos()
    {
        var data = new List<TipoSolicitudDisponibleDto>
        {
            new(TipoSolicitud.RequisicionPersonal.ToString(), "Requisicion de Personal", true, "Disponible"),
            new(TipoSolicitud.AccionPersonal.ToString(), "Accion de Personal", false, "Proximamente"),
            new(TipoSolicitud.Vacaciones.ToString(), "Solicitud de Vacaciones", false, "Proximamente")
        };

        return Ok(ApiResponse<List<TipoSolicitudDisponibleDto>>.Ok(data));
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
            .Include(x => x.RequisicionPersonal!).ThenInclude(x => x.DepartamentoSolicitado)
            .Include(x => x.RequisicionPersonal!).ThenInclude(x => x.ColaboradorReemplazado)
            .Include(x => x.RequisicionPersonal!).ThenInclude(x => x.TipoContrato)
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

    private async Task<ApproverSelection> ResolveApproverAsync(RequisicionPersonalRequestBase request, CancellationToken cancellationToken)
    {
        if (!request.DepartamentoResponsableId.HasValue)
        {
            return new ApproverSelection(request.LiderAprobadorUsuarioId, request.LiderAprobadorColaboradorId, null);
        }

        var responsable = await db.DepartamentoResponsables
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.DepartamentoResponsableId == request.DepartamentoResponsableId.Value, cancellationToken);

        return responsable is null
            ? new ApproverSelection(request.LiderAprobadorUsuarioId, request.LiderAprobadorColaboradorId, request.DepartamentoResponsableId)
            : new ApproverSelection(responsable.UsuarioResponsableId, responsable.ColaboradorResponsableId, responsable.DepartamentoResponsableId);
    }

    private async Task<string> GenerateCodigoSolicitudAsync(CancellationToken cancellationToken)
    {
        var year = DateTime.UtcNow.Year;
        var prefix = $"REQ-{year}-";
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
        return request.DepartamentoResponsableId.HasValue ||
            request.LiderAprobadorColaboradorId.HasValue ||
            request.LiderAprobadorUsuarioId.HasValue;
    }

    private static bool IsOperationalCollaborator(Colaborador colaborador)
    {
        return colaborador.IsActive && OperationalStatusCodes.Contains(colaborador.Estatus.Codigo);
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
            actions.Add("cerrar");
        }

        return actions;
    }
}
