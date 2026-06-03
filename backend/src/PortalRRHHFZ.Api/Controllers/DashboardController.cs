using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PortalRRHHFZ.Application.DTOs.Dashboard;
using PortalRRHHFZ.Application.Interfaces.Dashboard;

namespace PortalRRHHFZ.Api.Controllers;

[ApiController]
[Authorize(Policy = "RequireAdminOrRRHH")]
[Route("api/dashboard")]
public sealed class DashboardController(IDashboardService dashboardService) : ControllerBase
{
    [HttpGet("resumen")]
    public async Task<IActionResult> GetResumen(CancellationToken cancellationToken)
    {
        var response = await dashboardService.GetResumenAsync(cancellationToken);
        return Ok(response);
    }

    [HttpGet("vencimientos")]
    public async Task<IActionResult> GetVencimientos(CancellationToken cancellationToken)
    {
        var response = await dashboardService.GetVencimientosAsync(cancellationToken);
        return Ok(response);
    }

    [HttpGet("colaboradores-por-estatus")]
    public async Task<IActionResult> GetColaboradoresPorEstatus(CancellationToken cancellationToken)
    {
        var response = await dashboardService.GetColaboradoresPorEstatusAsync(cancellationToken);
        return Ok(response);
    }

    [HttpGet("colaboradores-por-departamento")]
    public async Task<IActionResult> GetColaboradoresPorDepartamento(CancellationToken cancellationToken)
    {
        var response = await dashboardService.GetColaboradoresPorDepartamentoAsync(cancellationToken);
        return Ok(response);
    }

    [HttpGet("altas-bajas")]
    public async Task<IActionResult> GetAltasBajas(
        [FromQuery] AltasBajasFilterRequest filters,
        CancellationToken cancellationToken)
    {
        var response = await dashboardService.GetAltasBajasAsync(filters, cancellationToken);
        return Ok(response);
    }

    [HttpGet("ultimos-movimientos")]
    public async Task<IActionResult> GetUltimosMovimientos(CancellationToken cancellationToken)
    {
        var response = await dashboardService.GetUltimosMovimientosAsync(cancellationToken);
        return Ok(response);
    }
}
