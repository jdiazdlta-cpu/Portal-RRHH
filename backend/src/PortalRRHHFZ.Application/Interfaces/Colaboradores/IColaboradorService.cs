using System.Security.Claims;
using PortalRRHHFZ.Application.Common;
using PortalRRHHFZ.Application.DTOs.Colaboradores;

namespace PortalRRHHFZ.Application.Interfaces.Colaboradores;

public interface IColaboradorService
{
    Task<ApiResponse<IReadOnlyCollection<ColaboradorListDto>>> GetAllAsync(ColaboradorFilterRequest filters, CancellationToken cancellationToken = default);
    Task<ApiResponse<ColaboradorDetailDto>> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<ApiResponse<ColaboradorPerfilDto>> GetPerfilAsync(int id, CancellationToken cancellationToken = default);
    Task<ApiResponse<ColaboradorDetailDto>> CreateAsync(CreateColaboradorRequest request, ClaimsPrincipal principal, CancellationToken cancellationToken = default);
    Task<ApiResponse<ColaboradorDetailDto>> UpdateAsync(int id, UpdateColaboradorRequest request, ClaimsPrincipal principal, CancellationToken cancellationToken = default);
    Task<ApiResponse<ColaboradorDetailDto>> ActivateAsync(int id, ClaimsPrincipal principal, CancellationToken cancellationToken = default);
    Task<ApiResponse<ColaboradorDetailDto>> DeactivateAsync(int id, ClaimsPrincipal principal, CancellationToken cancellationToken = default);
    Task<ApiResponse<IReadOnlyCollection<HistorialColaboradorDto>>> GetHistorialAsync(int id, CancellationToken cancellationToken = default);
}
