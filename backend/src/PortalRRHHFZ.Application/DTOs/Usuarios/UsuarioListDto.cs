namespace PortalRRHHFZ.Application.DTOs.Usuarios;

public sealed class UsuarioListDto
{
    public int UsuarioId { get; init; }
    public string NombreUsuario { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public string Rol { get; init; } = string.Empty;
    public bool IsActive { get; init; }
    public DateTime? UltimoAcceso { get; init; }
    public DateTime CreatedAt { get; init; }
}
