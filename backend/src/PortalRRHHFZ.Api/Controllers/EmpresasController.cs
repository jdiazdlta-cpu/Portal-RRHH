using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PortalRRHHFZ.Application.DTOs.Empresas;
using PortalRRHHFZ.Application.Interfaces.Empresas;

namespace PortalRRHHFZ.Api.Controllers;

[ApiController]
[Authorize(Policy = "RequireAdminOrRRHH")]
[Route("api/empresas")]
public sealed class EmpresasController(IEmpresaService empresaService) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        var response = await empresaService.GetAllAsync(cancellationToken);
        return Ok(response);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id, CancellationToken cancellationToken)
    {
        var response = await empresaService.GetByIdAsync(id, cancellationToken);
        return response.Success ? Ok(response) : NotFound(response);
    }

    [HttpPost]
    public async Task<IActionResult> Create(
        CreateEmpresaRequest request,
        CancellationToken cancellationToken)
    {
        var response = await empresaService.CreateAsync(
            request,
            User.Identity?.Name,
            cancellationToken);

        return response.Success
            ? Created($"/api/empresas/{response.Data?.EmpresaId}", response)
            : BadRequest(response);
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(
        int id,
        UpdateEmpresaRequest request,
        CancellationToken cancellationToken)
    {
        var response = await empresaService.UpdateAsync(
            id,
            request,
            User.Identity?.Name,
            cancellationToken);

        return response.Success ? Ok(response) : BadRequest(response);
    }

    [HttpPatch("{id:int}/activar")]
    public async Task<IActionResult> Activate(int id, CancellationToken cancellationToken)
    {
        var response = await empresaService.ActivateAsync(
            id,
            User.Identity?.Name,
            cancellationToken);

        return response.Success ? Ok(response) : BadRequest(response);
    }

    [HttpPatch("{id:int}/desactivar")]
    public async Task<IActionResult> Deactivate(int id, CancellationToken cancellationToken)
    {
        var response = await empresaService.DeactivateAsync(
            id,
            User.Identity?.Name,
            cancellationToken);

        return response.Success ? Ok(response) : BadRequest(response);
    }
}
