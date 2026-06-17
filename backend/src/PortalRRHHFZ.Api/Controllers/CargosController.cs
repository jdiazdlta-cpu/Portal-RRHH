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
[Route("api/cargos")]
public sealed class CargosController(AppDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Get([FromQuery] int? departamentoId, CancellationToken cancellationToken)
    {
        var query = db.Cargos.Include(x => x.Departamento).ThenInclude(x => x.Empresa).AsQueryable();
        if (departamentoId.HasValue)
        {
            query = query.Where(x => x.DepartamentoId == departamentoId.Value);
        }

        var data = await query.OrderBy(x => x.Nombre)
            .Select(x => new CargoDto(x.CargoId, x.DepartamentoId, x.Departamento.Nombre, x.Departamento.EmpresaId, x.Departamento.Empresa.Nombre, x.Nombre, x.IsActive))
            .ToListAsync(cancellationToken);
        return Ok(ApiResponse<List<CargoDto>>.Ok(data));
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id, CancellationToken cancellationToken)
    {
        var item = await db.Cargos.Include(x => x.Departamento).ThenInclude(x => x.Empresa).FirstOrDefaultAsync(x => x.CargoId == id, cancellationToken);
        return item is null
            ? NotFound(ApiResponse<object>.Fail("Cargo no encontrado."))
            : Ok(ApiResponse<CargoDto>.Ok(new CargoDto(item.CargoId, item.DepartamentoId, item.Departamento.Nombre, item.Departamento.EmpresaId, item.Departamento.Empresa.Nombre, item.Nombre, item.IsActive)));
    }

    [Authorize(Policy = AppPolicies.RequireAdmin)]
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] UpsertCargoRequest request, CancellationToken cancellationToken)
    {
        var validation = await ValidateAsync(request, cancellationToken);
        if (validation is not null)
        {
            return BadRequest(ApiResponse<object>.Fail(validation));
        }

        var item = new Cargo { DepartamentoId = request.DepartamentoId, Nombre = request.Nombre.Trim(), CreatedBy = User.Identity?.Name };
        db.Cargos.Add(item);
        await db.SaveChangesAsync(cancellationToken);
        await db.Entry(item).Reference(x => x.Departamento).LoadAsync(cancellationToken);
        await db.Entry(item.Departamento).Reference(x => x.Empresa).LoadAsync(cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = item.CargoId }, ApiResponse<CargoDto>.Ok(new CargoDto(item.CargoId, item.DepartamentoId, item.Departamento.Nombre, item.Departamento.EmpresaId, item.Departamento.Empresa.Nombre, item.Nombre, item.IsActive), "Cargo creado."));
    }

    [Authorize(Policy = AppPolicies.RequireAdmin)]
    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] UpsertCargoRequest request, CancellationToken cancellationToken)
    {
        var item = await db.Cargos.Include(x => x.Departamento).ThenInclude(x => x.Empresa).FirstOrDefaultAsync(x => x.CargoId == id, cancellationToken);
        if (item is null)
        {
            return NotFound(ApiResponse<object>.Fail("Cargo no encontrado."));
        }

        var validation = await ValidateAsync(request, cancellationToken);
        if (validation is not null)
        {
            return BadRequest(ApiResponse<object>.Fail(validation));
        }

        item.DepartamentoId = request.DepartamentoId;
        item.Nombre = request.Nombre.Trim();
        item.UpdatedBy = User.Identity?.Name;
        await db.SaveChangesAsync(cancellationToken);
        await db.Entry(item).Reference(x => x.Departamento).LoadAsync(cancellationToken);
        await db.Entry(item.Departamento).Reference(x => x.Empresa).LoadAsync(cancellationToken);
        return Ok(ApiResponse<CargoDto>.Ok(new CargoDto(item.CargoId, item.DepartamentoId, item.Departamento.Nombre, item.Departamento.EmpresaId, item.Departamento.Empresa.Nombre, item.Nombre, item.IsActive), "Cargo actualizado."));
    }

    [Authorize(Policy = AppPolicies.RequireAdmin)]
    [HttpPatch("{id:int}/activar")]
    public Task<IActionResult> Activar(int id, CancellationToken cancellationToken) => Toggle(id, true, cancellationToken);

    [Authorize(Policy = AppPolicies.RequireAdmin)]
    [HttpPatch("{id:int}/desactivar")]
    public Task<IActionResult> Desactivar(int id, CancellationToken cancellationToken) => Toggle(id, false, cancellationToken);

    private async Task<string?> ValidateAsync(UpsertCargoRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Nombre))
        {
            return "Nombre es obligatorio.";
        }

        return await db.Departamentos.AnyAsync(x => x.DepartamentoId == request.DepartamentoId && x.IsActive, cancellationToken)
            ? null
            : "Departamento no valido.";
    }

    private async Task<IActionResult> Toggle(int id, bool active, CancellationToken cancellationToken)
    {
        var item = await db.Cargos.Include(x => x.Departamento).ThenInclude(x => x.Empresa).FirstOrDefaultAsync(x => x.CargoId == id, cancellationToken);
        if (item is null)
        {
            return NotFound(ApiResponse<object>.Fail("Cargo no encontrado."));
        }

        item.IsActive = active;
        item.UpdatedBy = User.Identity?.Name;
        await db.SaveChangesAsync(cancellationToken);
        return Ok(ApiResponse<CargoDto>.Ok(new CargoDto(item.CargoId, item.DepartamentoId, item.Departamento.Nombre, item.Departamento.EmpresaId, item.Departamento.Empresa.Nombre, item.Nombre, item.IsActive), active ? "Cargo activado." : "Cargo desactivado."));
    }
}
