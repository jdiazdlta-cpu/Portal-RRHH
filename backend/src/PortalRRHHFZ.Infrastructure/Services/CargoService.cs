using Microsoft.EntityFrameworkCore;
using PortalRRHHFZ.Application.Common;
using PortalRRHHFZ.Application.DTOs.Cargos;
using PortalRRHHFZ.Application.Interfaces.Cargos;
using PortalRRHHFZ.Domain.Entities;
using PortalRRHHFZ.Infrastructure.Data;

namespace PortalRRHHFZ.Infrastructure.Services;

public sealed class CargoService(AppDbContext dbContext) : ICargoService
{
    public async Task<ApiResponse<IReadOnlyCollection<CargoListDto>>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        var cargos = await dbContext.Cargos
            .Include(cargo => cargo.Departamento)
            .ThenInclude(departamento => departamento.Empresa)
            .AsNoTracking()
            .OrderBy(cargo => cargo.Departamento.Empresa.Nombre)
            .ThenBy(cargo => cargo.Departamento.Nombre)
            .ThenBy(cargo => cargo.Nombre)
            .Select(cargo => new CargoListDto
            {
                CargoId = cargo.CargoId,
                DepartamentoId = cargo.DepartamentoId,
                DepartamentoNombre = cargo.Departamento.Nombre,
                EmpresaId = cargo.Departamento.EmpresaId,
                EmpresaNombre = cargo.Departamento.Empresa.Nombre,
                Nombre = cargo.Nombre,
                IsActive = cargo.IsActive,
                CreatedAt = cargo.CreatedAt
            })
            .ToListAsync(cancellationToken);

        return ApiResponse<IReadOnlyCollection<CargoListDto>>.Ok(cargos);
    }

    public async Task<ApiResponse<CargoDetailDto>> GetByIdAsync(
        int id,
        CancellationToken cancellationToken = default)
    {
        var cargo = await dbContext.Cargos
            .Include(item => item.Departamento)
            .ThenInclude(departamento => departamento.Empresa)
            .AsNoTracking()
            .SingleOrDefaultAsync(item => item.CargoId == id, cancellationToken);

        return cargo is null
            ? ApiResponse<CargoDetailDto>.Fail("Cargo no encontrado.")
            : ApiResponse<CargoDetailDto>.Ok(ToDetailDto(cargo));
    }

    public async Task<ApiResponse<CargoDetailDto>> CreateAsync(
        CreateCargoRequest request,
        string? currentUser,
        CancellationToken cancellationToken = default)
    {
        var errors = await ValidateAsync(request.DepartamentoId, request.Nombre, null, cancellationToken);

        if (errors.Count > 0)
        {
            return ApiResponse<CargoDetailDto>.Fail("No fue posible crear el cargo.", errors);
        }

        var cargo = new Cargo
        {
            DepartamentoId = request.DepartamentoId,
            Nombre = request.Nombre.Trim(),
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = currentUser
        };

        dbContext.Cargos.Add(cargo);
        await dbContext.SaveChangesAsync(cancellationToken);
        await LoadParentsAsync(cargo, cancellationToken);

        return ApiResponse<CargoDetailDto>.Ok(ToDetailDto(cargo), "Cargo creado correctamente.");
    }

    public async Task<ApiResponse<CargoDetailDto>> UpdateAsync(
        int id,
        UpdateCargoRequest request,
        string? currentUser,
        CancellationToken cancellationToken = default)
    {
        var cargo = await dbContext.Cargos
            .Include(item => item.Departamento)
            .ThenInclude(departamento => departamento.Empresa)
            .SingleOrDefaultAsync(item => item.CargoId == id, cancellationToken);

        if (cargo is null)
        {
            return ApiResponse<CargoDetailDto>.Fail("Cargo no encontrado.");
        }

        var errors = await ValidateAsync(request.DepartamentoId, request.Nombre, id, cancellationToken);

        if (errors.Count > 0)
        {
            return ApiResponse<CargoDetailDto>.Fail("No fue posible actualizar el cargo.", errors);
        }

        cargo.DepartamentoId = request.DepartamentoId;
        cargo.Nombre = request.Nombre.Trim();
        cargo.UpdatedAt = DateTime.UtcNow;
        cargo.UpdatedBy = currentUser;

        await dbContext.SaveChangesAsync(cancellationToken);
        await LoadParentsAsync(cargo, cancellationToken);

        return ApiResponse<CargoDetailDto>.Ok(ToDetailDto(cargo), "Cargo actualizado correctamente.");
    }

    public async Task<ApiResponse<CargoDetailDto>> ActivateAsync(
        int id,
        string? currentUser,
        CancellationToken cancellationToken = default)
    {
        var cargo = await dbContext.Cargos
            .Include(item => item.Departamento)
            .ThenInclude(departamento => departamento.Empresa)
            .SingleOrDefaultAsync(item => item.CargoId == id, cancellationToken);

        if (cargo is null)
        {
            return ApiResponse<CargoDetailDto>.Fail("Cargo no encontrado.");
        }

        var errors = await ValidateParentAndDuplicateAsync(
            cargo.DepartamentoId,
            cargo.Nombre,
            cargo.CargoId,
            cancellationToken);

        if (errors.Count > 0)
        {
            return ApiResponse<CargoDetailDto>.Fail("No fue posible activar el cargo.", errors);
        }

        cargo.IsActive = true;
        cargo.UpdatedAt = DateTime.UtcNow;
        cargo.UpdatedBy = currentUser;

        await dbContext.SaveChangesAsync(cancellationToken);

        return ApiResponse<CargoDetailDto>.Ok(ToDetailDto(cargo), "Cargo activado correctamente.");
    }

    public async Task<ApiResponse<CargoDetailDto>> DeactivateAsync(
        int id,
        string? currentUser,
        CancellationToken cancellationToken = default)
    {
        var cargo = await dbContext.Cargos
            .Include(item => item.Departamento)
            .ThenInclude(departamento => departamento.Empresa)
            .SingleOrDefaultAsync(item => item.CargoId == id, cancellationToken);

        if (cargo is null)
        {
            return ApiResponse<CargoDetailDto>.Fail("Cargo no encontrado.");
        }

        cargo.IsActive = false;
        cargo.UpdatedAt = DateTime.UtcNow;
        cargo.UpdatedBy = currentUser;

        await dbContext.SaveChangesAsync(cancellationToken);

        return ApiResponse<CargoDetailDto>.Ok(ToDetailDto(cargo), "Cargo desactivado correctamente.");
    }

    private async Task<List<string>> ValidateAsync(
        int departamentoId,
        string nombre,
        int? cargoId,
        CancellationToken cancellationToken)
    {
        var errors = new List<string>();

        if (departamentoId <= 0)
        {
            errors.Add("DepartamentoId es obligatorio.");
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
            departamentoId,
            nombre,
            cargoId,
            cancellationToken));

        return errors;
    }

    private async Task<List<string>> ValidateParentAndDuplicateAsync(
        int departamentoId,
        string nombre,
        int? cargoId,
        CancellationToken cancellationToken)
    {
        var errors = new List<string>();
        var departamentoActivo = await dbContext.Departamentos.AnyAsync(
            departamento =>
                departamento.DepartamentoId == departamentoId
                && departamento.IsActive
                && departamento.Empresa.IsActive,
            cancellationToken);

        if (!departamentoActivo)
        {
            errors.Add("DepartamentoId no existe o esta inactivo.");
            return errors;
        }

        var normalizedName = nombre.Trim().ToLower();
        var duplicateExists = await dbContext.Cargos.AnyAsync(
            cargo =>
                cargo.DepartamentoId == departamentoId
                && cargo.Nombre.ToLower() == normalizedName
                && (!cargoId.HasValue || cargo.CargoId != cargoId.Value),
            cancellationToken);

        if (duplicateExists)
        {
            errors.Add("Ya existe un cargo con ese nombre dentro del departamento.");
        }

        return errors;
    }

    private async Task LoadParentsAsync(Cargo cargo, CancellationToken cancellationToken)
    {
        await dbContext.Entry(cargo).Reference(item => item.Departamento).LoadAsync(cancellationToken);
        await dbContext.Entry(cargo.Departamento).Reference(item => item.Empresa).LoadAsync(cancellationToken);
    }

    private static CargoDetailDto ToDetailDto(Cargo cargo)
    {
        return new CargoDetailDto
        {
            CargoId = cargo.CargoId,
            DepartamentoId = cargo.DepartamentoId,
            DepartamentoNombre = cargo.Departamento.Nombre,
            EmpresaId = cargo.Departamento.EmpresaId,
            EmpresaNombre = cargo.Departamento.Empresa.Nombre,
            Nombre = cargo.Nombre,
            IsActive = cargo.IsActive,
            CreatedAt = cargo.CreatedAt,
            UpdatedAt = cargo.UpdatedAt,
            CreatedBy = cargo.CreatedBy,
            UpdatedBy = cargo.UpdatedBy
        };
    }
}
