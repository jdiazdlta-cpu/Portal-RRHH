using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PortalRRHHFZ.Application.DTOs.Colaboradores;
using PortalRRHHFZ.Application.Interfaces.Colaboradores;

namespace PortalRRHHFZ.Api.Controllers;

[ApiController]
[Authorize(Policy = "RequireAdminOrRRHH")]
[Route("api/colaboradores")]
public sealed class ColaboradoresController(IColaboradorService colaboradorService) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] ColaboradorFilterRequest filters,
        CancellationToken cancellationToken)
    {
        var response = await colaboradorService.GetAllAsync(filters, cancellationToken);
        return Ok(response);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id, CancellationToken cancellationToken)
    {
        var response = await colaboradorService.GetByIdAsync(id, cancellationToken);
        return response.Success ? Ok(response) : NotFound(response);
    }

    [HttpGet("{id:int}/perfil")]
    public async Task<IActionResult> GetPerfil(int id, CancellationToken cancellationToken)
    {
        var response = await colaboradorService.GetPerfilAsync(id, cancellationToken);
        return response.Success ? Ok(response) : NotFound(response);
    }

    [HttpPost]
    public async Task<IActionResult> Create(
        CreateColaboradorRequest request,
        CancellationToken cancellationToken)
    {
        var response = await colaboradorService.CreateAsync(
            request,
            User,
            cancellationToken);

        return response.Success
            ? Created($"/api/colaboradores/{response.Data?.ColaboradorId}", response)
            : BadRequest(response);
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(
        int id,
        UpdateColaboradorRequest request,
        CancellationToken cancellationToken)
    {
        var response = await colaboradorService.UpdateAsync(
            id,
            request,
            User,
            cancellationToken);

        return response.Success ? Ok(response) : BadRequest(response);
    }

    [HttpPatch("{id:int}/activar")]
    public async Task<IActionResult> Activate(int id, CancellationToken cancellationToken)
    {
        var response = await colaboradorService.ActivateAsync(id, User, cancellationToken);
        return response.Success ? Ok(response) : BadRequest(response);
    }

    [HttpPatch("{id:int}/desactivar")]
    public async Task<IActionResult> Deactivate(int id, CancellationToken cancellationToken)
    {
        var response = await colaboradorService.DeactivateAsync(id, User, cancellationToken);
        return response.Success ? Ok(response) : BadRequest(response);
    }

    [HttpGet("{id:int}/historial")]
    public async Task<IActionResult> GetHistorial(int id, CancellationToken cancellationToken)
    {
        var response = await colaboradorService.GetHistorialAsync(id, cancellationToken);
        return response.Success ? Ok(response) : NotFound(response);
    }
}
