namespace PortalRRHHFZ.Application.DTOs.Auth;

public sealed class AuthUserDto
{
    public int UserId { get; init; }
    public string Email { get; init; } = string.Empty;
    public string NombreUsuario { get; init; } = string.Empty;
    public string Rol { get; init; } = string.Empty;
}
