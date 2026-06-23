using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PortalRRHHFZ.Application.Common;
using PortalRRHHFZ.Application.DTOs;
using PortalRRHHFZ.Domain.Constants;
using PortalRRHHFZ.Infrastructure.Data;

namespace PortalRRHHFZ.Api.Controllers;

[ApiController]
[Authorize(Policy = AppPolicies.RequireSolicitudes)]
[Route("api/catalogos")]
public sealed class CatalogosController(AppDbContext db) : ControllerBase
{
    [HttpGet("roles")]
    [Authorize(Policy = AppPolicies.RequireAdmin)]
    public async Task<IActionResult> Roles(CancellationToken cancellationToken)
    {
        var data = await db.Roles.Where(x => x.IsActive).OrderBy(x => x.RolId).Select(x => new RolDto(x.RolId, x.Nombre, x.Descripcion)).ToListAsync(cancellationToken);
        return Ok(ApiResponse<List<RolDto>>.Ok(data));
    }

    [HttpGet("empresas")]
    public async Task<IActionResult> Empresas(CancellationToken cancellationToken)
    {
        var data = await db.Empresas.Where(x => x.IsActive).OrderBy(x => x.Nombre).Select(x => new CatalogoItemDto(x.EmpresaId, x.Nombre)).ToListAsync(cancellationToken);
        return Ok(ApiResponse<List<CatalogoItemDto>>.Ok(data));
    }

    [HttpGet("departamentos")]
    public async Task<IActionResult> Departamentos([FromQuery] int? empresaId, CancellationToken cancellationToken)
    {
        var query = db.Departamentos.Where(x => x.IsActive);
        if (empresaId.HasValue)
        {
            query = query.Where(x => x.EmpresaId == empresaId.Value);
        }

        var data = await query.OrderBy(x => x.Nombre).Select(x => new CatalogoItemDto(x.DepartamentoId, x.Nombre)).ToListAsync(cancellationToken);
        return Ok(ApiResponse<List<CatalogoItemDto>>.Ok(data));
    }

    [HttpGet("cargos")]
    public async Task<IActionResult> Cargos([FromQuery] int? departamentoId, CancellationToken cancellationToken)
    {
        var query = db.Cargos.Where(x => x.IsActive);
        if (departamentoId.HasValue)
        {
            query = query.Where(x => x.DepartamentoId == departamentoId.Value);
        }

        var data = await query.OrderBy(x => x.Nombre).Select(x => new CatalogoItemDto(x.CargoId, x.Nombre)).ToListAsync(cancellationToken);
        return Ok(ApiResponse<List<CatalogoItemDto>>.Ok(data));
    }

    [HttpGet("tipos-contrato")]
    public async Task<IActionResult> TiposContrato(CancellationToken cancellationToken)
    {
        var data = await db.TiposContrato
            .Where(x => x.IsActive)
            .OrderBy(x => x.TipoContratoId)
            .Select(x => new CatalogoItemDto(x.TipoContratoId, x.Nombre, null, x.RequiereFechaVencimiento))
            .ToListAsync(cancellationToken);
        return Ok(ApiResponse<List<CatalogoItemDto>>.Ok(data));
    }

    [HttpGet("estatus-colaborador")]
    public async Task<IActionResult> Estatus(CancellationToken cancellationToken)
    {
        var data = await db.EstatusColaborador
            .Where(x => x.IsActive)
            .OrderBy(x => x.EstatusId)
            .Select(x => new CatalogoItemDto(x.EstatusId, x.Nombre, x.Codigo))
            .ToListAsync(cancellationToken);
        return Ok(ApiResponse<List<CatalogoItemDto>>.Ok(data));
    }

    [HttpGet("motivos-salida")]
    public async Task<IActionResult> MotivosSalida(CancellationToken cancellationToken)
    {
        var data = await db.MotivosSalida.Where(x => x.IsActive).OrderBy(x => x.Nombre).Select(x => new CatalogoItemDto(x.MotivoSalidaId, x.Nombre)).ToListAsync(cancellationToken);
        return Ok(ApiResponse<List<CatalogoItemDto>>.Ok(data));
    }

    [HttpGet("tipos-documento")]
    public async Task<IActionResult> TiposDocumento(CancellationToken cancellationToken)
    {
        var data = await db.TiposDocumento
            .Where(x => x.IsActive)
            .OrderBy(x => x.TipoDocumentoId)
            .Select(x => new CatalogoItemDto(x.TipoDocumentoId, x.Nombre, null, null, x.TieneVencimientoSugerido))
            .ToListAsync(cancellationToken);
        return Ok(ApiResponse<List<CatalogoItemDto>>.Ok(data));
    }
}
