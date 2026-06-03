using Microsoft.EntityFrameworkCore;
using PortalRRHHFZ.Application.Common;
using PortalRRHHFZ.Application.DTOs.Empresas;
using PortalRRHHFZ.Application.Interfaces.Empresas;
using PortalRRHHFZ.Domain.Entities;
using PortalRRHHFZ.Infrastructure.Data;

namespace PortalRRHHFZ.Infrastructure.Services;

public sealed class EmpresaService(AppDbContext dbContext) : IEmpresaService
{
    public async Task<ApiResponse<IReadOnlyCollection<EmpresaListDto>>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        var empresas = await dbContext.Empresas
            .AsNoTracking()
            .OrderBy(empresa => empresa.Nombre)
            .Select(empresa => new EmpresaListDto
            {
                EmpresaId = empresa.EmpresaId,
                Nombre = empresa.Nombre,
                Ruc = empresa.Ruc,
                IsActive = empresa.IsActive,
                CreatedAt = empresa.CreatedAt
            })
            .ToListAsync(cancellationToken);

        return ApiResponse<IReadOnlyCollection<EmpresaListDto>>.Ok(empresas);
    }

    public async Task<ApiResponse<EmpresaDetailDto>> GetByIdAsync(
        int id,
        CancellationToken cancellationToken = default)
    {
        var empresa = await dbContext.Empresas
            .AsNoTracking()
            .SingleOrDefaultAsync(item => item.EmpresaId == id, cancellationToken);

        return empresa is null
            ? ApiResponse<EmpresaDetailDto>.Fail("Empresa no encontrada.")
            : ApiResponse<EmpresaDetailDto>.Ok(ToDetailDto(empresa));
    }

    public async Task<ApiResponse<EmpresaDetailDto>> CreateAsync(
        CreateEmpresaRequest request,
        string? currentUser,
        CancellationToken cancellationToken = default)
    {
        var errors = await ValidateAsync(request.Nombre, null, cancellationToken);

        if (errors.Count > 0)
        {
            return ApiResponse<EmpresaDetailDto>.Fail("No fue posible crear la empresa.", errors);
        }

        var empresa = new Empresa
        {
            Nombre = request.Nombre.Trim(),
            Ruc = NormalizeNullable(request.Ruc),
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = currentUser
        };

        dbContext.Empresas.Add(empresa);
        await dbContext.SaveChangesAsync(cancellationToken);

        return ApiResponse<EmpresaDetailDto>.Ok(ToDetailDto(empresa), "Empresa creada correctamente.");
    }

    public async Task<ApiResponse<EmpresaDetailDto>> UpdateAsync(
        int id,
        UpdateEmpresaRequest request,
        string? currentUser,
        CancellationToken cancellationToken = default)
    {
        var empresa = await dbContext.Empresas
            .SingleOrDefaultAsync(item => item.EmpresaId == id, cancellationToken);

        if (empresa is null)
        {
            return ApiResponse<EmpresaDetailDto>.Fail("Empresa no encontrada.");
        }

        var errors = await ValidateAsync(request.Nombre, id, cancellationToken);

        if (errors.Count > 0)
        {
            return ApiResponse<EmpresaDetailDto>.Fail("No fue posible actualizar la empresa.", errors);
        }

        empresa.Nombre = request.Nombre.Trim();
        empresa.Ruc = NormalizeNullable(request.Ruc);
        empresa.UpdatedAt = DateTime.UtcNow;
        empresa.UpdatedBy = currentUser;

        await dbContext.SaveChangesAsync(cancellationToken);

        return ApiResponse<EmpresaDetailDto>.Ok(ToDetailDto(empresa), "Empresa actualizada correctamente.");
    }

    public async Task<ApiResponse<EmpresaDetailDto>> ActivateAsync(
        int id,
        string? currentUser,
        CancellationToken cancellationToken = default)
    {
        var empresa = await dbContext.Empresas
            .SingleOrDefaultAsync(item => item.EmpresaId == id, cancellationToken);

        if (empresa is null)
        {
            return ApiResponse<EmpresaDetailDto>.Fail("Empresa no encontrada.");
        }

        var errors = await ValidateActiveDuplicateAsync(empresa.Nombre, id, cancellationToken);

        if (errors.Count > 0)
        {
            return ApiResponse<EmpresaDetailDto>.Fail("No fue posible activar la empresa.", errors);
        }

        empresa.IsActive = true;
        empresa.UpdatedAt = DateTime.UtcNow;
        empresa.UpdatedBy = currentUser;

        await dbContext.SaveChangesAsync(cancellationToken);

        return ApiResponse<EmpresaDetailDto>.Ok(ToDetailDto(empresa), "Empresa activada correctamente.");
    }

    public async Task<ApiResponse<EmpresaDetailDto>> DeactivateAsync(
        int id,
        string? currentUser,
        CancellationToken cancellationToken = default)
    {
        var empresa = await dbContext.Empresas
            .SingleOrDefaultAsync(item => item.EmpresaId == id, cancellationToken);

        if (empresa is null)
        {
            return ApiResponse<EmpresaDetailDto>.Fail("Empresa no encontrada.");
        }

        empresa.IsActive = false;
        empresa.UpdatedAt = DateTime.UtcNow;
        empresa.UpdatedBy = currentUser;

        await dbContext.SaveChangesAsync(cancellationToken);

        return ApiResponse<EmpresaDetailDto>.Ok(ToDetailDto(empresa), "Empresa desactivada correctamente.");
    }

    private async Task<List<string>> ValidateAsync(
        string nombre,
        int? empresaId,
        CancellationToken cancellationToken)
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(nombre))
        {
            errors.Add("Nombre es obligatorio.");
            return errors;
        }

        errors.AddRange(await ValidateActiveDuplicateAsync(nombre, empresaId, cancellationToken));

        return errors;
    }

    private async Task<List<string>> ValidateActiveDuplicateAsync(
        string nombre,
        int? empresaId,
        CancellationToken cancellationToken)
    {
        var normalizedName = nombre.Trim().ToLower();
        var duplicateExists = await dbContext.Empresas.AnyAsync(
            empresa =>
                empresa.IsActive
                && empresa.Nombre.ToLower() == normalizedName
                && (!empresaId.HasValue || empresa.EmpresaId != empresaId.Value),
            cancellationToken);

        return duplicateExists
            ? ["Ya existe una empresa activa con ese nombre."]
            : [];
    }

    private static EmpresaDetailDto ToDetailDto(Empresa empresa)
    {
        return new EmpresaDetailDto
        {
            EmpresaId = empresa.EmpresaId,
            Nombre = empresa.Nombre,
            Ruc = empresa.Ruc,
            IsActive = empresa.IsActive,
            CreatedAt = empresa.CreatedAt,
            UpdatedAt = empresa.UpdatedAt,
            CreatedBy = empresa.CreatedBy,
            UpdatedBy = empresa.UpdatedBy
        };
    }

    private static string? NormalizeNullable(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}
