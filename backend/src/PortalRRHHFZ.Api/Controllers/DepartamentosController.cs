using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PortalRRHHFZ.Application.DTOs.Departamentos;
using PortalRRHHFZ.Application.Interfaces.Departamentos;

namespace PortalRRHHFZ.Api.Controllers;

[ApiController]
[Authorize(Policy = "RequireAdminOrRRHH")]
[Route("api/departamentos")]
public sealed class DepartamentosController(IDepartamentoService departamentoService) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        var response = await departamentoService.GetAllAsync(cancellationToken);
        return Ok(response);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id, CancellationToken cancellationToken)
    {
        var response = await departamentoService.GetByIdAsync(id, cancellationToken);
        return response.Success ? Ok(response) : NotFound(response);
    }

    [HttpPost]
    public async Task<IActionResult> Create(
        CreateDepartamentoRequest request,
        CancellationToken cancellationToken)
    {
        var response = await departamentoService.CreateAsync(
            request,
            User.Identity?.Name,
            cancellationToken);

        return response.Success
            ? Created($"/api/departamentos/{response.Data?.DepartamentoId}", response)
            : BadRequest(response);
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(
        int id,
        UpdateDepartamentoRequest request,
        CancellationToken cancellationToken)
    {
        var response = await departamentoService.UpdateAsync(
            id,
            request,
            User.Identity?.Name,
            cancellationToken);

        return response.Success ? Ok(response) : BadRequest(response);
    }

    [HttpPatch("{id:int}/activar")]
    public async Task<IActionResult> Activate(int id, CancellationToken cancellationToken)
    {
        var response = await departamentoService.ActivateAsync(
            id,
            User.Identity?.Name,
            cancellationToken);

        return response.Success ? Ok(response) : BadRequest(response);
    }

    [HttpPatch("{id:int}/desactivar")]
    public async Task<IActionResult> Deactivate(int id, CancellationToken cancellationToken)
    {
        var response = await departamentoService.DeactivateAsync(
            id,
            User.Identity?.Name,
            cancellationToken);

        return response.Success ? Ok(response) : BadRequest(response);
    }
}
