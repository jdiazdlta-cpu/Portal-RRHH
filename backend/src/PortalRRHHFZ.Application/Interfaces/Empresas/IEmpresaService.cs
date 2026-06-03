using PortalRRHHFZ.Application.Common;
using PortalRRHHFZ.Application.DTOs.Empresas;

namespace PortalRRHHFZ.Application.Interfaces.Empresas;

public interface IEmpresaService
{
    Task<ApiResponse<IReadOnlyCollection<EmpresaListDto>>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<ApiResponse<EmpresaDetailDto>> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<ApiResponse<EmpresaDetailDto>> CreateAsync(CreateEmpresaRequest request, string? currentUser, CancellationToken cancellationToken = default);
    Task<ApiResponse<EmpresaDetailDto>> UpdateAsync(int id, UpdateEmpresaRequest request, string? currentUser, CancellationToken cancellationToken = default);
    Task<ApiResponse<EmpresaDetailDto>> ActivateAsync(int id, string? currentUser, CancellationToken cancellationToken = default);
    Task<ApiResponse<EmpresaDetailDto>> DeactivateAsync(int id, string? currentUser, CancellationToken cancellationToken = default);
}
