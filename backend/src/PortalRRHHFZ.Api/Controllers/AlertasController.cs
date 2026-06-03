using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PortalRRHHFZ.Application.DTOs.Alertas;
using PortalRRHHFZ.Application.Interfaces.Alertas;

namespace PortalRRHHFZ.Api.Controllers;

[ApiController]
[Authorize(Policy = "RequireAdminOrRRHH")]
[Route("api/alertas")]
public sealed class AlertasController(IAlertaService alertaService) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] AlertaFilterRequest filters,
        CancellationToken cancellationToken)
    {
        var response = await alertaService.GetAllAsync(filters, cancellationToken);
        return Ok(response);
    }

    [HttpGet("resumen")]
    public async Task<IActionResult> GetResumen(CancellationToken cancellationToken)
    {
        var response = await alertaService.GetResumenAsync(cancellationToken);
        return Ok(response);
    }

    [HttpPatch("{id:int}/gestionar")]
    public async Task<IActionResult> Gestionar(
        int id,
        GestionarAlertaRequest request,
        CancellationToken cancellationToken)
    {
        var response = await alertaService.GestionarAsync(id, request, User, cancellationToken);
        return response.Success ? Ok(response) : BadRequest(response);
    }

    [HttpPatch("{id:int}/ignorar")]
    public async Task<IActionResult> Ignorar(
        int id,
        GestionarAlertaRequest request,
        CancellationToken cancellationToken)
    {
        var response = await alertaService.IgnorarAsync(id, request, User, cancellationToken);
        return response.Success ? Ok(response) : BadRequest(response);
    }

    [HttpPost("recalcular")]
    public async Task<IActionResult> Recalcular(CancellationToken cancellationToken)
    {
        var response = await alertaService.RecalcularAsync(User, cancellationToken);
        return response.Success ? Ok(response) : BadRequest(response);
    }
}
