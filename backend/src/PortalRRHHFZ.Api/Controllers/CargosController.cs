using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PortalRRHHFZ.Application.DTOs.Cargos;
using PortalRRHHFZ.Application.Interfaces.Cargos;

namespace PortalRRHHFZ.Api.Controllers;

[ApiController]
[Authorize(Policy = "RequireAdminOrRRHH")]
[Route("api/cargos")]
public sealed class CargosController(ICargoService cargoService) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        var response = await cargoService.GetAllAsync(cancellationToken);
        return Ok(response);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id, CancellationToken cancellationToken)
    {
        var response = await cargoService.GetByIdAsync(id, cancellationToken);
        return response.Success ? Ok(response) : NotFound(response);
    }

    [HttpPost]
    public async Task<IActionResult> Create(
        CreateCargoRequest request,
        CancellationToken cancellationToken)
    {
        var response = await cargoService.CreateAsync(
            request,
            User.Identity?.Name,
            cancellationToken);

        return response.Success
            ? Created($"/api/cargos/{response.Data?.CargoId}", response)
            : BadRequest(response);
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(
        int id,
        UpdateCargoRequest request,
        CancellationToken cancellationToken)
    {
        var response = await cargoService.UpdateAsync(
            id,
            request,
            User.Identity?.Name,
            cancellationToken);

        return response.Success ? Ok(response) : BadRequest(response);
    }

    [HttpPatch("{id:int}/activar")]
    public async Task<IActionResult> Activate(int id, CancellationToken cancellationToken)
    {
        var response = await cargoService.ActivateAsync(
            id,
            User.Identity?.Name,
            cancellationToken);

        return response.Success ? Ok(response) : BadRequest(response);
    }

    [HttpPatch("{id:int}/desactivar")]
    public async Task<IActionResult> Deactivate(int id, CancellationToken cancellationToken)
    {
        var response = await cargoService.DeactivateAsync(
            id,
            User.Identity?.Name,
            cancellationToken);

        return response.Success ? Ok(response) : BadRequest(response);
    }
}
