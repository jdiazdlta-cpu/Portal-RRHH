using PortalRRHHFZ.Domain.Entities;

namespace PortalRRHHFZ.Application.Interfaces;

public sealed record JwtTokenResult(string Token, DateTime ExpiresAt);

public interface IJwtTokenService
{
    JwtTokenResult Generate(Usuario usuario);
}
