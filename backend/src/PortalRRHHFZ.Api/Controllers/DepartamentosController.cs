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
[Route("api/departamentos")]
public sealed class DepartamentosController(AppDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Get([FromQuery] int? empresaId, CancellationToken cancellationToken)
    {
        var query = db.Departamentos.Include(x => x.Empresa).AsQueryable();
        if (empresaId.HasValue)
        {
            query = query.Where(x => x.EmpresaId == empresaId.Value);
        }

        var data = await query.OrderBy(x => x.Nombre)
            .Select(x => new DepartamentoDto(x.DepartamentoId, x.EmpresaId, x.Empresa.Nombre, x.Nombre, x.IsActive))
            .ToListAsync(cancellationToken);
        return Ok(ApiResponse<List<DepartamentoDto>>.Ok(data));
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id, CancellationToken cancellationToken)
    {
        var item = await db.Departamentos.Include(x => x.Empresa).FirstOrDefaultAsync(x => x.DepartamentoId == id, cancellationToken);
        return item is null
            ? NotFound(ApiResponse<object>.Fail("Departamento no encontrado."))
            : Ok(ApiResponse<DepartamentoDto>.Ok(new DepartamentoDto(item.DepartamentoId, item.EmpresaId, item.Empresa.Nombre, item.Nombre, item.IsActive)));
    }

    [Authorize(Policy = AppPolicies.RequireAdmin)]
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] UpsertDepartamentoRequest request, CancellationToken cancellationToken)
    {
        var validation = await ValidateAsync(request, cancellationToken);
        if (validation is not null)
        {
            return BadRequest(ApiResponse<object>.Fail(validation));
        }

        var item = new Departamento { EmpresaId = request.EmpresaId, Nombre = request.Nombre.Trim(), CreatedBy = User.Identity?.Name };
        db.Departamentos.Add(item);
        await db.SaveChangesAsync(cancellationToken);
        await db.Entry(item).Reference(x => x.Empresa).LoadAsync(cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = item.DepartamentoId }, ApiResponse<DepartamentoDto>.Ok(new DepartamentoDto(item.DepartamentoId, item.EmpresaId, item.Empresa.Nombre, item.Nombre, item.IsActive), "Departamento creado."));
    }

    [Authorize(Policy = AppPolicies.RequireAdmin)]
    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] UpsertDepartamentoRequest request, CancellationToken cancellationToken)
    {
        var item = await db.Departamentos.Include(x => x.Empresa).FirstOrDefaultAsync(x => x.DepartamentoId == id, cancellationToken);
        if (item is null)
        {
            return NotFound(ApiResponse<object>.Fail("Departamento no encontrado."));
        }

        var validation = await ValidateAsync(request, cancellationToken);
        if (validation is not null)
        {
            return BadRequest(ApiResponse<object>.Fail(validation));
        }

        item.EmpresaId = request.EmpresaId;
        item.Nombre = request.Nombre.Trim();
        item.UpdatedBy = User.Identity?.Name;
        await db.SaveChangesAsync(cancellationToken);
        await db.Entry(item).Reference(x => x.Empresa).LoadAsync(cancellationToken);
        return Ok(ApiResponse<DepartamentoDto>.Ok(new DepartamentoDto(item.DepartamentoId, item.EmpresaId, item.Empresa.Nombre, item.Nombre, item.IsActive), "Departamento actualizado."));
    }

    [Authorize(Policy = AppPolicies.RequireAdmin)]
    [HttpPatch("{id:int}/activar")]
    public Task<IActionResult> Activar(int id, CancellationToken cancellationToken) => Toggle(id, true, cancellationToken);

    [Authorize(Policy = AppPolicies.RequireAdmin)]
    [HttpPatch("{id:int}/desactivar")]
    public Task<IActionResult> Desactivar(int id, CancellationToken cancellationToken) => Toggle(id, false, cancellationToken);

    private async Task<string?> ValidateAsync(UpsertDepartamentoRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Nombre))
        {
            return "Nombre es obligatorio.";
        }

        return await db.Empresas.AnyAsync(x => x.EmpresaId == request.EmpresaId && x.IsActive, cancellationToken)
            ? null
            : "Empresa no valida.";
    }

    private async Task<IActionResult> Toggle(int id, bool active, CancellationToken cancellationToken)
    {
        var item = await db.Departamentos.Include(x => x.Empresa).FirstOrDefaultAsync(x => x.DepartamentoId == id, cancellationToken);
        if (item is null)
        {
            return NotFound(ApiResponse<object>.Fail("Departamento no encontrado."));
        }

        item.IsActive = active;
        item.UpdatedBy = User.Identity?.Name;
        await db.SaveChangesAsync(cancellationToken);
        return Ok(ApiResponse<DepartamentoDto>.Ok(new DepartamentoDto(item.DepartamentoId, item.EmpresaId, item.Empresa.Nombre, item.Nombre, item.IsActive), active ? "Departamento activado." : "Departamento desactivado."));
    }
}
