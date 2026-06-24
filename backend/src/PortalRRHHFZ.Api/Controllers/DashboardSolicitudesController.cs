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
[Route("api/dashboard")]
public sealed class DashboardSolicitudesController(AppDbContext db) : ControllerBase
{
    [HttpGet("solicitudes-resumen")]
    public async Task<IActionResult> SolicitudesResumen(CancellationToken cancellationToken)
    {
        var solicitudes = await ApplySolicitudScope(db.Solicitudes
                .AsNoTracking()
                .AsSplitQuery()
                .Include(x => x.SolicitanteUsuario)
                .Include(x => x.Empresa)
                .Include(x => x.Departamento)
                .Include(x => x.AccionPersonal)
                .Include(x => x.Aprobaciones).ThenInclude(x => x.UsuarioAprobador)
                .Include(x => x.Aprobaciones).ThenInclude(x => x.ColaboradorAprobador))
            .OrderByDescending(x => x.FechaSolicitud)
            .ThenByDescending(x => x.SolicitudId)
            .ToListAsync(cancellationToken);

        var currentMonthStart = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
        var nextMonthStart = currentMonthStart.AddMonths(1);
        var abiertas = solicitudes.Where(IsOpen).ToList();
        var apPendientesEjecucion = solicitudes
            .Where(x => x.TipoSolicitud == TipoSolicitud.AccionPersonal && x.Estado == EstadoSolicitud.Aprobada && x.AccionPersonal?.Ejecutada != true)
            .OrderByDescending(x => x.UpdatedAt ?? x.FechaSolicitud)
            .ThenByDescending(x => x.SolicitudId)
            .ToList();

        var dto = new DashboardSolicitudesResumenDto(
            abiertas.Count,
            solicitudes.Count(x => x.Estado == EstadoSolicitud.PendienteAprobacionLider),
            solicitudes.Count(x => x.Estado == EstadoSolicitud.PendienteRevisionRRHH),
            solicitudes.Count(x => x.Estado == EstadoSolicitud.Devuelta),
            CountInCurrentMonth(solicitudes, EstadoSolicitud.Rechazada, currentMonthStart, nextMonthStart),
            CountInCurrentMonth(solicitudes, EstadoSolicitud.Aprobada, currentMonthStart, nextMonthStart),
            CountInCurrentMonth(solicitudes, EstadoSolicitud.Ejecutada, currentMonthStart, nextMonthStart),
            apPendientesEjecucion.Count,
            solicitudes.Count(x => x.AccionPersonal?.AlertaOrigenId is not null),
            abiertas.Count(x => x.TipoSolicitud == TipoSolicitud.RequisicionPersonal),
            abiertas.Count(x => x.TipoSolicitud == TipoSolicitud.AccionPersonal),
            solicitudes.GroupBy(x => x.TipoSolicitud).Select(x => new ChartItemDto(FormatSolicitudType(x.Key), x.Count())).OrderByDescending(x => x.Value).ToList(),
            solicitudes.GroupBy(x => x.Estado).Select(x => new ChartItemDto(FormatEstado(x.Key), x.Count())).OrderByDescending(x => x.Value).ToList(),
            solicitudes.Take(10).Select(ToDashboardItem).ToList(),
            solicitudes.Where(IsPendingMyAction).Take(10).Select(ToDashboardItem).ToList(),
            apPendientesEjecucion.Take(10).Select(ToDashboardItem).ToList());

        return Ok(ApiResponse<DashboardSolicitudesResumenDto>.Ok(dto));
    }

    private IQueryable<Solicitud> ApplySolicitudScope(IQueryable<Solicitud> query)
    {
        if (CanManageAll())
        {
            return query;
        }

        var currentUserId = User.CurrentUserId();
        return query.Where(x => x.SolicitanteUsuarioId == currentUserId || x.Aprobaciones.Any(a => a.UsuarioAprobadorId == currentUserId));
    }

    private bool IsPendingMyAction(Solicitud solicitud)
    {
        var currentUserId = User.CurrentUserId();
        if ((solicitud.Estado is EstadoSolicitud.Borrador or EstadoSolicitud.Devuelta) && solicitud.SolicitanteUsuarioId == currentUserId)
        {
            return true;
        }

        if (solicitud.Aprobaciones.Any(x => x.Estado == EstadoAprobacion.Pendiente && x.UsuarioAprobadorId == currentUserId))
        {
            return true;
        }

        return CanManageAll() &&
            (solicitud.Estado == EstadoSolicitud.PendienteRevisionRRHH ||
             solicitud.TipoSolicitud == TipoSolicitud.AccionPersonal && solicitud.Estado == EstadoSolicitud.Aprobada && solicitud.AccionPersonal?.Ejecutada != true);
    }

    private static bool IsOpen(Solicitud solicitud)
        => solicitud.Estado is not EstadoSolicitud.Rechazada and not EstadoSolicitud.Cancelada and not EstadoSolicitud.Cerrada and not EstadoSolicitud.Ejecutada;

    private static int CountInCurrentMonth(IEnumerable<Solicitud> solicitudes, EstadoSolicitud estado, DateTime start, DateTime end)
        => solicitudes.Count(x => x.Estado == estado && EffectiveDate(x) >= start && EffectiveDate(x) < end);

    private static DateTime EffectiveDate(Solicitud solicitud)
        => solicitud.UpdatedAt ?? solicitud.FechaSolicitud;

    private static DashboardSolicitudItemDto ToDashboardItem(Solicitud solicitud)
    {
        return new DashboardSolicitudItemDto(
            solicitud.SolicitudId,
            solicitud.CodigoSolicitud,
            FormatSolicitudType(solicitud.TipoSolicitud),
            FormatEstado(solicitud.Estado),
            solicitud.SolicitanteUsuario.NombreUsuario,
            solicitud.Empresa?.Nombre,
            solicitud.Departamento?.Nombre,
            solicitud.FechaSolicitud,
            solicitud.UpdatedAt,
            GetPendingLabel(solicitud),
            $"/solicitudes?solicitudId={solicitud.SolicitudId}");
    }

    private static string? GetPendingLabel(Solicitud solicitud)
    {
        var pending = solicitud.Aprobaciones
            .OrderBy(x => x.Orden)
            .FirstOrDefault(x => x.Estado == EstadoAprobacion.Pendiente);

        if (pending is null)
        {
            return solicitud.Estado switch
            {
                EstadoSolicitud.Borrador => "Solicitante",
                EstadoSolicitud.Devuelta => "Solicitante",
                EstadoSolicitud.Aprobada when solicitud.TipoSolicitud == TipoSolicitud.AccionPersonal && solicitud.AccionPersonal?.Ejecutada != true => "RRHH - ejecucion",
                _ => null
            };
        }

        var name = pending.ColaboradorAprobador?.NombreCompleto()
            ?? pending.UsuarioAprobador?.NombreUsuario
            ?? pending.RolAprobador;

        return $"{pending.Etapa}: {name}";
    }

    private bool CanManageAll()
        => User.IsInRole(AppRoles.Admin) || User.IsInRole(AppRoles.RRHH);

    private static string FormatSolicitudType(TipoSolicitud value)
        => value switch
        {
            TipoSolicitud.RequisicionPersonal => "Requisicion de Personal",
            TipoSolicitud.AccionPersonal => "Accion de Personal",
            TipoSolicitud.Vacaciones => "Vacaciones",
            _ => value.ToString()
        };

    private static string FormatEstado(EstadoSolicitud value)
        => value switch
        {
            EstadoSolicitud.PendienteAprobacionLider => "Pendiente lider",
            EstadoSolicitud.PendienteRevisionRRHH => "Pendiente RRHH",
            _ => value.ToString()
        };
}
