using System.Security.Claims;
using PortalRRHHFZ.Application.Common;
using PortalRRHHFZ.Application.DTOs.Usuarios;

namespace PortalRRHHFZ.Application.Interfaces.Usuarios;

public interface IUsuarioService
{
    Task<ApiResponse<IReadOnlyCollection<UsuarioListDto>>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<ApiResponse<UsuarioDetailDto>> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<ApiResponse<UsuarioDetailDto>> CreateAsync(CreateUsuarioRequest request, string? currentUser, CancellationToken cancellationToken = default);
    Task<ApiResponse<UsuarioDetailDto>> UpdateAsync(int id, UpdateUsuarioRequest request, string? currentUser, CancellationToken cancellationToken = default);
    Task<ApiResponse<UsuarioDetailDto>> ActivateAsync(int id, string? currentUser, CancellationToken cancellationToken = default);
    Task<ApiResponse<UsuarioDetailDto>> DeactivateAsync(int id, ClaimsPrincipal principal, CancellationToken cancellationToken = default);
    Task<ApiResponse<UsuarioDetailDto>> ResetPasswordAsync(int id, ResetPasswordRequest request, string? currentUser, CancellationToken cancellationToken = default);
}
