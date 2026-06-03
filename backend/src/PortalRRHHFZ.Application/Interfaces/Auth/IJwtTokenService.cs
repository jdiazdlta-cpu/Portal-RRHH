using PortalRRHHFZ.Application.DTOs.Auth;
using PortalRRHHFZ.Domain.Entities;

namespace PortalRRHHFZ.Application.Interfaces.Auth;

public interface IJwtTokenService
{
    LoginResponse CreateToken(Usuario usuario);
}
