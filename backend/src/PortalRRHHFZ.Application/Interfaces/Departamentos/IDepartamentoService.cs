using PortalRRHHFZ.Application.Common;
using PortalRRHHFZ.Application.DTOs.Departamentos;

namespace PortalRRHHFZ.Application.Interfaces.Departamentos;

public interface IDepartamentoService
{
    Task<ApiResponse<IReadOnlyCollection<DepartamentoListDto>>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<ApiResponse<DepartamentoDetailDto>> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<ApiResponse<DepartamentoDetailDto>> CreateAsync(CreateDepartamentoRequest request, string? currentUser, CancellationToken cancellationToken = default);
    Task<ApiResponse<DepartamentoDetailDto>> UpdateAsync(int id, UpdateDepartamentoRequest request, string? currentUser, CancellationToken cancellationToken = default);
    Task<ApiResponse<DepartamentoDetailDto>> ActivateAsync(int id, string? currentUser, CancellationToken cancellationToken = default);
    Task<ApiResponse<DepartamentoDetailDto>> DeactivateAsync(int id, string? currentUser, CancellationToken cancellationToken = default);
}
