using Microsoft.EntityFrameworkCore;
using PortalRRHHFZ.Application.Common;
using PortalRRHHFZ.Application.DTOs.Catalogos;
using PortalRRHHFZ.Application.Interfaces.Catalogos;
using PortalRRHHFZ.Infrastructure.Data;

namespace PortalRRHHFZ.Infrastructure.Services;

public sealed class CatalogoService(AppDbContext dbContext) : ICatalogoService
{
    public async Task<ApiResponse<IReadOnlyCollection<RolCatalogoDto>>> GetRolesAsync(
        CancellationToken cancellationToken = default)
    {
        var roles = await dbContext.Roles
            .AsNoTracking()
            .Where(rol => rol.IsActive)
            .OrderBy(rol => rol.Nombre)
            .Select(rol => new RolCatalogoDto
            {
                RolId = rol.RolId,
                Nombre = rol.Nombre,
                Descripcion = rol.Descripcion
            })
            .ToListAsync(cancellationToken);

        return ApiResponse<IReadOnlyCollection<RolCatalogoDto>>.Ok(roles);
    }

    public async Task<ApiResponse<IReadOnlyCollection<EmpresaCatalogoDto>>> GetEmpresasAsync(
        CancellationToken cancellationToken = default)
    {
        var empresas = await dbContext.Empresas
            .AsNoTracking()
            .Where(empresa => empresa.IsActive)
            .OrderBy(empresa => empresa.Nombre)
            .Select(empresa => new EmpresaCatalogoDto
            {
                EmpresaId = empresa.EmpresaId,
                Nombre = empresa.Nombre,
                Ruc = empresa.Ruc
            })
            .ToListAsync(cancellationToken);

        return ApiResponse<IReadOnlyCollection<EmpresaCatalogoDto>>.Ok(empresas);
    }

    public async Task<ApiResponse<IReadOnlyCollection<DepartamentoCatalogoDto>>> GetDepartamentosAsync(
        int? empresaId,
        CancellationToken cancellationToken = default)
    {
        var query = dbContext.Departamentos
            .AsNoTracking()
            .Where(departamento => departamento.IsActive && departamento.Empresa.IsActive);

        if (empresaId.HasValue)
        {
            query = query.Where(departamento => departamento.EmpresaId == empresaId.Value);
        }

        var departamentos = await query
            .OrderBy(departamento => departamento.Empresa.Nombre)
            .ThenBy(departamento => departamento.Nombre)
            .Select(departamento => new DepartamentoCatalogoDto
            {
                DepartamentoId = departamento.DepartamentoId,
                EmpresaId = departamento.EmpresaId,
                EmpresaNombre = departamento.Empresa.Nombre,
                Nombre = departamento.Nombre
            })
            .ToListAsync(cancellationToken);

        return ApiResponse<IReadOnlyCollection<DepartamentoCatalogoDto>>.Ok(departamentos);
    }

    public async Task<ApiResponse<IReadOnlyCollection<CargoCatalogoDto>>> GetCargosAsync(
        int? departamentoId,
        CancellationToken cancellationToken = default)
    {
        var query = dbContext.Cargos
            .AsNoTracking()
            .Where(cargo =>
                cargo.IsActive
                && cargo.Departamento.IsActive
                && cargo.Departamento.Empresa.IsActive);

        if (departamentoId.HasValue)
        {
            query = query.Where(cargo => cargo.DepartamentoId == departamentoId.Value);
        }

        var cargos = await query
            .OrderBy(cargo => cargo.Departamento.Empresa.Nombre)
            .ThenBy(cargo => cargo.Departamento.Nombre)
            .ThenBy(cargo => cargo.Nombre)
            .Select(cargo => new CargoCatalogoDto
            {
                CargoId = cargo.CargoId,
                DepartamentoId = cargo.DepartamentoId,
                DepartamentoNombre = cargo.Departamento.Nombre,
                EmpresaId = cargo.Departamento.EmpresaId,
                EmpresaNombre = cargo.Departamento.Empresa.Nombre,
                Nombre = cargo.Nombre
            })
            .ToListAsync(cancellationToken);

        return ApiResponse<IReadOnlyCollection<CargoCatalogoDto>>.Ok(cargos);
    }

    public async Task<ApiResponse<IReadOnlyCollection<TipoContratoCatalogoDto>>> GetTiposContratoAsync(
        CancellationToken cancellationToken = default)
    {
        var tiposContrato = await dbContext.TiposContrato
            .AsNoTracking()
            .Where(tipoContrato => tipoContrato.IsActive)
            .OrderBy(tipoContrato => tipoContrato.Nombre)
            .Select(tipoContrato => new TipoContratoCatalogoDto
            {
                TipoContratoId = tipoContrato.TipoContratoId,
                Nombre = tipoContrato.Nombre,
                RequiereFechaVencimiento = tipoContrato.RequiereFechaVencimiento
            })
            .ToListAsync(cancellationToken);

        return ApiResponse<IReadOnlyCollection<TipoContratoCatalogoDto>>.Ok(tiposContrato);
    }

    public async Task<ApiResponse<IReadOnlyCollection<EstatusColaboradorCatalogoDto>>> GetEstatusColaboradorAsync(
        CancellationToken cancellationToken = default)
    {
        var estatus = await dbContext.EstatusColaborador
            .AsNoTracking()
            .Where(estatus => estatus.IsActive)
            .OrderBy(estatus => estatus.Nombre)
            .Select(estatus => new EstatusColaboradorCatalogoDto
            {
                EstatusId = estatus.EstatusId,
                Nombre = estatus.Nombre,
                Codigo = estatus.Codigo
            })
            .ToListAsync(cancellationToken);

        return ApiResponse<IReadOnlyCollection<EstatusColaboradorCatalogoDto>>.Ok(estatus);
    }

    public async Task<ApiResponse<IReadOnlyCollection<MotivoSalidaCatalogoDto>>> GetMotivosSalidaAsync(
        CancellationToken cancellationToken = default)
    {
        var motivos = await dbContext.MotivosSalida
            .AsNoTracking()
            .Where(motivo => motivo.IsActive)
            .OrderBy(motivo => motivo.Nombre)
            .Select(motivo => new MotivoSalidaCatalogoDto
            {
                MotivoSalidaId = motivo.MotivoSalidaId,
                Nombre = motivo.Nombre
            })
            .ToListAsync(cancellationToken);

        return ApiResponse<IReadOnlyCollection<MotivoSalidaCatalogoDto>>.Ok(motivos);
    }

    public async Task<ApiResponse<IReadOnlyCollection<TipoDocumentoCatalogoDto>>> GetTiposDocumentoAsync(
        CancellationToken cancellationToken = default)
    {
        var tiposDocumento = await dbContext.TiposDocumento
            .AsNoTracking()
            .Where(tipoDocumento => tipoDocumento.IsActive)
            .OrderBy(tipoDocumento => tipoDocumento.Nombre)
            .Select(tipoDocumento => new TipoDocumentoCatalogoDto
            {
                TipoDocumentoId = tipoDocumento.TipoDocumentoId,
                Nombre = tipoDocumento.Nombre,
                TieneVencimientoSugerido = tipoDocumento.TieneVencimientoSugerido
            })
            .ToListAsync(cancellationToken);

        return ApiResponse<IReadOnlyCollection<TipoDocumentoCatalogoDto>>.Ok(tiposDocumento);
    }
}
