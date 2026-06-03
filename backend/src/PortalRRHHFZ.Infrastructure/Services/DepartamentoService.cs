using Microsoft.EntityFrameworkCore;
using PortalRRHHFZ.Application.Common;
using PortalRRHHFZ.Application.DTOs.Departamentos;
using PortalRRHHFZ.Application.Interfaces.Departamentos;
using PortalRRHHFZ.Domain.Entities;
using PortalRRHHFZ.Infrastructure.Data;

namespace PortalRRHHFZ.Infrastructure.Services;

public sealed class DepartamentoService(AppDbContext dbContext) : IDepartamentoService
{
    public async Task<ApiResponse<IReadOnlyCollection<DepartamentoListDto>>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        var departamentos = await dbContext.Departamentos
            .Include(departamento => departamento.Empresa)
            .AsNoTracking()
            .OrderBy(departamento => departamento.Empresa.Nombre)
            .ThenBy(departamento => departamento.Nombre)
            .Select(departamento => new DepartamentoListDto
            {
                DepartamentoId = departamento.DepartamentoId,
                EmpresaId = departamento.EmpresaId,
                EmpresaNombre = departamento.Empresa.Nombre,
                Nombre = departamento.Nombre,
                IsActive = departamento.IsActive,
                CreatedAt = departamento.CreatedAt
            })
            .ToListAsync(cancellationToken);

        return ApiResponse<IReadOnlyCollection<DepartamentoListDto>>.Ok(departamentos);
    }

    public async Task<ApiResponse<DepartamentoDetailDto>> GetByIdAsync(
        int id,
        CancellationToken cancellationToken = default)
    {
        var departamento = await dbContext.Departamentos
            .Include(item => item.Empresa)
            .AsNoTracking()
            .SingleOrDefaultAsync(item => item.DepartamentoId == id, cancellationToken);

        return departamento is null
            ? ApiResponse<DepartamentoDetailDto>.Fail("Departamento no encontrado.")
            : ApiResponse<DepartamentoDetailDto>.Ok(ToDetailDto(departamento));
    }

    public async Task<ApiResponse<DepartamentoDetailDto>> CreateAsync(
        CreateDepartamentoRequest request,
        string? currentUser,
        CancellationToken cancellationToken = default)
    {
        var errors = await ValidateAsync(request.EmpresaId, request.Nombre, null, cancellationToken);

        if (errors.Count > 0)
        {
            return ApiResponse<DepartamentoDetailDto>.Fail("No fue posible crear el departamento.", errors);
        }

        var departamento = new Departamento
        {
            EmpresaId = request.EmpresaId,
            Nombre = request.Nombre.Trim(),
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = currentUser
        };

        dbContext.Departamentos.Add(departamento);
        await dbContext.SaveChangesAsync(cancellationToken);
        await dbContext.Entry(departamento).Reference(item => item.Empresa).LoadAsync(cancellationToken);

        return ApiResponse<DepartamentoDetailDto>.Ok(ToDetailDto(departamento), "Departamento creado correctamente.");
    }

    public async Task<ApiResponse<DepartamentoDetailDto>> UpdateAsync(
        int id,
        UpdateDepartamentoRequest request,
        string? currentUser,
        CancellationToken cancellationToken = default)
    {
        var departamento = await dbContext.Departamentos
            .Include(item => item.Empresa)
            .SingleOrDefaultAsync(item => item.DepartamentoId == id, cancellationToken);

        if (departamento is null)
        {
            return ApiResponse<DepartamentoDetailDto>.Fail("Departamento no encontrado.");
        }

        var errors = await ValidateAsync(request.EmpresaId, request.Nombre, id, cancellationToken);

        if (errors.Count > 0)
        {
            return ApiResponse<DepartamentoDetailDto>.Fail("No fue posible actualizar el departamento.", errors);
        }

        departamento.EmpresaId = request.EmpresaId;
        departamento.Nombre = request.Nombre.Trim();
        departamento.UpdatedAt = DateTime.UtcNow;
        departamento.UpdatedBy = currentUser;

        await dbContext.SaveChangesAsync(cancellationToken);
        await dbContext.Entry(departamento).Reference(item => item.Empresa).LoadAsync(cancellationToken);

        return ApiResponse<DepartamentoDetailDto>.Ok(ToDetailDto(departamento), "Departamento actualizado correctamente.");
    }

    public async Task<ApiResponse<DepartamentoDetailDto>> ActivateAsync(
        int id,
        string? currentUser,
        CancellationToken cancellationToken = default)
    {
        var departamento = await dbContext.Departamentos
            .Include(item => item.Empresa)
            .SingleOrDefaultAsync(item => item.DepartamentoId == id, cancellationToken);

        if (departamento is null)
        {
            return ApiResponse<DepartamentoDetailDto>.Fail("Departamento no encontrado.");
        }

        var errors = await ValidateParentAndDuplicateAsync(
            departamento.EmpresaId,
            departamento.Nombre,
            departamento.DepartamentoId,
            cancellationToken);

        if (errors.Count > 0)
        {
            return ApiResponse<DepartamentoDetailDto>.Fail("No fue posible activar el departamento.", errors);
        }

        departamento.IsActive = true;
        departamento.UpdatedAt = DateTime.UtcNow;
        departamento.UpdatedBy = currentUser;

        await dbContext.SaveChangesAsync(cancellationToken);

        return ApiResponse<DepartamentoDetailDto>.Ok(ToDetailDto(departamento), "Departamento activado correctamente.");
    }

    public async Task<ApiResponse<DepartamentoDetailDto>> DeactivateAsync(
        int id,
        string? currentUser,
        CancellationToken cancellationToken = default)
    {
        var departamento = await dbContext.Departamentos
            .Include(item => item.Empresa)
            .SingleOrDefaultAsync(item => item.DepartamentoId == id, cancellationToken);

        if (departamento is null)
        {
            return ApiResponse<DepartamentoDetailDto>.Fail("Departamento no encontrado.");
        }

        departamento.IsActive = false;
        departamento.UpdatedAt = DateTime.UtcNow;
        departamento.UpdatedBy = currentUser;

        await dbContext.SaveChangesAsync(cancellationToken);

        return ApiResponse<DepartamentoDetailDto>.Ok(ToDetailDto(departamento), "Departamento desactivado correctamente.");
    }

    private async Task<List<string>> ValidateAsync(
        int empresaId,
        string nombre,
        int? departamentoId,
        CancellationToken cancellationToken)
    {
        var errors = new List<string>();

        if (empresaId <= 0)
        {
            errors.Add("EmpresaId es obligatorio.");
        }

        if (string.IsNullOrWhiteSpace(nombre))
        {
            errors.Add("Nombre es obligatorio.");
        }

        if (errors.Count > 0)
        {
            return errors;
        }

        errors.AddRange(await ValidateParentAndDuplicateAsync(
            empresaId,
            nombre,
            departamentoId,
            cancellationToken));

        return errors;
    }

    private async Task<List<string>> ValidateParentAndDuplicateAsync(
        int empresaId,
        string nombre,
        int? departamentoId,
        CancellationToken cancellationToken)
    {
        var errors = new List<string>();
        var empresaActiva = await dbContext.Empresas.AnyAsync(
            empresa => empresa.EmpresaId == empresaId && empresa.IsActive,
            cancellationToken);

        if (!empresaActiva)
        {
            errors.Add("EmpresaId no existe o esta inactiva.");
            return errors;
        }

        var normalizedName = nombre.Trim().ToLower();
        var duplicateExists = await dbContext.Departamentos.AnyAsync(
            departamento =>
                departamento.EmpresaId == empresaId
                && departamento.Nombre.ToLower() == normalizedName
                && (!departamentoId.HasValue || departamento.DepartamentoId != departamentoId.Value),
            cancellationToken);

        if (duplicateExists)
        {
            errors.Add("Ya existe un departamento con ese nombre dentro de la empresa.");
        }

        return errors;
    }

    private static DepartamentoDetailDto ToDetailDto(Departamento departamento)
    {
        return new DepartamentoDetailDto
        {
            DepartamentoId = departamento.DepartamentoId,
            EmpresaId = departamento.EmpresaId,
            EmpresaNombre = departamento.Empresa.Nombre,
            Nombre = departamento.Nombre,
            IsActive = departamento.IsActive,
            CreatedAt = departamento.CreatedAt,
            UpdatedAt = departamento.UpdatedAt,
            CreatedBy = departamento.CreatedBy,
            UpdatedBy = departamento.UpdatedBy
        };
    }
}
