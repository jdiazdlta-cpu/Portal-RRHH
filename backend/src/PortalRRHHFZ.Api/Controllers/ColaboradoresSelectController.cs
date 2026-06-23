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
[Authorize(Policy = AppPolicies.RequireSolicitudes)]
[Route("api/colaboradores")]
public sealed class ColaboradoresSelectController(AppDbContext db) : ControllerBase
{
    private static readonly string[] OperationalStatusCodes = ["A", "V", "S"];

    [HttpGet("select")]
    public async Task<IActionResult> Select(
        [FromQuery] int? empresaId,
        [FromQuery] int? departamentoId,
        [FromQuery] int? cargoId,
        [FromQuery] bool soloActivos = true,
        [FromQuery] string? search = null,
        [FromQuery] int take = 50,
        CancellationToken cancellationToken = default)
    {
        var canViewCompensation = CanViewCompensation();
        var query = LaborQuery(soloActivos)
            .AsNoTracking()
            .AsQueryable();

        if (empresaId.HasValue)
        {
            query = query.Where(x => x.EmpresaId == empresaId.Value);
        }

        if (departamentoId.HasValue)
        {
            query = query.Where(x => x.DepartamentoId == departamentoId.Value);
        }

        if (cargoId.HasValue)
        {
            query = query.Where(x => x.CargoId == cargoId.Value);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(x =>
                x.NoEmpleado.Contains(term) ||
                x.Cedula.Contains(term) ||
                x.PrimerNombre.Contains(term) ||
                x.PrimerApellido.Contains(term) ||
                (x.SegundoNombre != null && x.SegundoNombre.Contains(term)) ||
                (x.SegundoApellido != null && x.SegundoApellido.Contains(term)));
        }

        var colaboradores = await query
            .OrderBy(x => x.PrimerApellido)
            .ThenBy(x => x.PrimerNombre)
            .Take(Math.Clamp(take, 1, 100))
            .ToListAsync(cancellationToken);

        var data = colaboradores.Select(x => ToSelectDto(x, canViewCompensation)).ToList();
        return Ok(ApiResponse<List<ColaboradorSelectDto>>.Ok(data));
    }

    [HttpGet("{id:int}/resumen-laboral")]
    public async Task<IActionResult> ResumenLaboral(int id, CancellationToken cancellationToken)
    {
        var colaborador = await LaborQuery(false)
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.ColaboradorId == id, cancellationToken);

        if (colaborador is null)
        {
            return NotFound(ApiResponse<object>.Fail("Colaborador no encontrado."));
        }

        var item = ToResumenDto(colaborador, CanViewCompensation());
        return Ok(ApiResponse<ColaboradorResumenLaboralDto>.Ok(item));
    }

    private IQueryable<Colaborador> LaborQuery(bool soloActivos)
    {
        var query = db.Colaboradores
            .Include(x => x.Empresa)
            .Include(x => x.Departamento)
            .Include(x => x.Cargo)
            .Include(x => x.JefeInmediato)
            .Include(x => x.TipoContrato)
            .Include(x => x.Estatus)
            .AsQueryable();

        if (soloActivos)
        {
            query = query.Where(x => x.IsActive && OperationalStatusCodes.Contains(x.Estatus.Codigo));
        }

        return query;
    }

    private ColaboradorSelectDto ToSelectDto(Colaborador colaborador, bool includeCompensation)
    {
        return new ColaboradorSelectDto(
            colaborador.ColaboradorId,
            colaborador.NoEmpleado,
            colaborador.NombreCompleto(),
            colaborador.Cedula,
            colaborador.EmpresaId,
            colaborador.Empresa.Nombre,
            colaborador.DepartamentoId,
            colaborador.Departamento.Nombre,
            colaborador.CargoId,
            colaborador.Cargo.Nombre,
            colaborador.JefeInmediatoId,
            colaborador.JefeInmediato?.NombreCompleto(),
            colaborador.TipoContratoId,
            colaborador.TipoContrato.Nombre,
            colaborador.EstatusId,
            colaborador.Estatus.Nombre,
            colaborador.Estatus.Codigo,
            includeCompensation ? colaborador.Salario : null,
            includeCompensation ? colaborador.Viaticos : null,
            includeCompensation ? colaborador.GastosRepresentacion : null);
    }

    private ColaboradorResumenLaboralDto ToResumenDto(Colaborador colaborador, bool includeCompensation)
    {
        return new ColaboradorResumenLaboralDto(
            colaborador.ColaboradorId,
            colaborador.NoEmpleado,
            colaborador.NombreCompleto(),
            colaborador.Cedula,
            colaborador.EmpresaId,
            colaborador.Empresa.Nombre,
            colaborador.DepartamentoId,
            colaborador.Departamento.Nombre,
            colaborador.CargoId,
            colaborador.Cargo.Nombre,
            colaborador.JefeInmediatoId,
            colaborador.JefeInmediato?.NombreCompleto(),
            colaborador.TipoContratoId,
            colaborador.TipoContrato.Nombre,
            colaborador.EstatusId,
            colaborador.Estatus.Nombre,
            colaborador.Estatus.Codigo,
            includeCompensation ? colaborador.Salario : null,
            includeCompensation ? colaborador.Viaticos : null,
            includeCompensation ? colaborador.GastosRepresentacion : null);
    }

    private bool CanViewCompensation()
    {
        return User.IsInRole(AppRoles.Admin) || User.IsInRole(AppRoles.RRHH);
    }
}
