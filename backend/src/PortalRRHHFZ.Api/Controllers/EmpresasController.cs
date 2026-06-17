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
[Authorize(Policy = AppPolicies.RequireAdminOrRRHH)]
[Route("api/empresas")]
public sealed class EmpresasController(AppDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Get(CancellationToken cancellationToken)
    {
        var data = await db.Empresas.OrderBy(x => x.Nombre).Select(x => new EmpresaDto(x.EmpresaId, x.Nombre, x.Ruc, x.IsActive)).ToListAsync(cancellationToken);
        return Ok(ApiResponse<List<EmpresaDto>>.Ok(data));
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id, CancellationToken cancellationToken)
    {
        var item = await db.Empresas.FirstOrDefaultAsync(x => x.EmpresaId == id, cancellationToken);
        return item is null
            ? NotFound(ApiResponse<object>.Fail("Empresa no encontrada."))
            : Ok(ApiResponse<EmpresaDto>.Ok(new EmpresaDto(item.EmpresaId, item.Nombre, item.Ruc, item.IsActive)));
    }

    [Authorize(Policy = AppPolicies.RequireAdmin)]
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] UpsertEmpresaRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Nombre))
        {
            return BadRequest(ApiResponse<object>.Fail("Nombre es obligatorio."));
        }

        var item = new Empresa { Nombre = request.Nombre.Trim(), Ruc = request.Ruc?.Trim(), CreatedBy = User.Identity?.Name };
        db.Empresas.Add(item);
        await db.SaveChangesAsync(cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = item.EmpresaId }, ApiResponse<EmpresaDto>.Ok(new EmpresaDto(item.EmpresaId, item.Nombre, item.Ruc, item.IsActive), "Empresa creada."));
    }

    [Authorize(Policy = AppPolicies.RequireAdmin)]
    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] UpsertEmpresaRequest request, CancellationToken cancellationToken)
    {
        var item = await db.Empresas.FirstOrDefaultAsync(x => x.EmpresaId == id, cancellationToken);
        if (item is null)
        {
            return NotFound(ApiResponse<object>.Fail("Empresa no encontrada."));
        }

        item.Nombre = request.Nombre.Trim();
        item.Ruc = request.Ruc?.Trim();
        item.UpdatedBy = User.Identity?.Name;
        await db.SaveChangesAsync(cancellationToken);
        return Ok(ApiResponse<EmpresaDto>.Ok(new EmpresaDto(item.EmpresaId, item.Nombre, item.Ruc, item.IsActive), "Empresa actualizada."));
    }

    [Authorize(Policy = AppPolicies.RequireAdmin)]
    [HttpPatch("{id:int}/activar")]
    public Task<IActionResult> Activar(int id, CancellationToken cancellationToken) => Toggle(id, true, cancellationToken);

    [Authorize(Policy = AppPolicies.RequireAdmin)]
    [HttpPatch("{id:int}/desactivar")]
    public Task<IActionResult> Desactivar(int id, CancellationToken cancellationToken) => Toggle(id, false, cancellationToken);

    private async Task<IActionResult> Toggle(int id, bool active, CancellationToken cancellationToken)
    {
        var item = await db.Empresas.FirstOrDefaultAsync(x => x.EmpresaId == id, cancellationToken);
        if (item is null)
        {
            return NotFound(ApiResponse<object>.Fail("Empresa no encontrada."));
        }

        item.IsActive = active;
        item.UpdatedBy = User.Identity?.Name;
        await db.SaveChangesAsync(cancellationToken);
        return Ok(ApiResponse<EmpresaDto>.Ok(new EmpresaDto(item.EmpresaId, item.Nombre, item.Ruc, item.IsActive), active ? "Empresa activada." : "Empresa desactivada."));
    }
}
