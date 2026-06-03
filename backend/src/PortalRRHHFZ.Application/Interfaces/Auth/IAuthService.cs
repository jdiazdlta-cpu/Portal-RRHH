using System.Security.Claims;
using PortalRRHHFZ.Application.Common;
using PortalRRHHFZ.Application.DTOs.Auth;

namespace PortalRRHHFZ.Application.Interfaces.Auth;

public interface IAuthService
{
    Task<ApiResponse<LoginResponse>> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default);
    Task<ApiResponse<AuthUserDto>> GetCurrentUserAsync(ClaimsPrincipal principal, CancellationToken cancellationToken = default);
}
