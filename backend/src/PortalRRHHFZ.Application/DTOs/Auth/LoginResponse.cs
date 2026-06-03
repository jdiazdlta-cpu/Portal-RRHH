namespace PortalRRHHFZ.Application.DTOs.Auth;

public sealed class LoginResponse
{
    public string Token { get; init; } = string.Empty;
    public DateTime ExpiresAt { get; init; }
    public AuthUserDto User { get; init; } = new();
}
