using PortalRRHHFZ.Application.Common;
using PortalRRHHFZ.Application.DTOs.Cargos;

namespace PortalRRHHFZ.Application.Interfaces.Cargos;

public interface ICargoService
{
    Task<ApiResponse<IReadOnlyCollection<CargoListDto>>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<ApiResponse<CargoDetailDto>> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<ApiResponse<CargoDetailDto>> CreateAsync(CreateCargoRequest request, string? currentUser, CancellationToken cancellationToken = default);
    Task<ApiResponse<CargoDetailDto>> UpdateAsync(int id, UpdateCargoRequest request, string? currentUser, CancellationToken cancellationToken = default);
    Task<ApiResponse<CargoDetailDto>> ActivateAsync(int id, string? currentUser, CancellationToken cancellationToken = default);
    Task<ApiResponse<CargoDetailDto>> DeactivateAsync(int id, string? currentUser, CancellationToken cancellationToken = default);
}
