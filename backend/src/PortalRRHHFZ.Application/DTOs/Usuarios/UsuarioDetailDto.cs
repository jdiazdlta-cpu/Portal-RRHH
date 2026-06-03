namespace PortalRRHHFZ.Application.DTOs.Usuarios;

public sealed class UsuarioDetailDto
{
    public int UsuarioId { get; init; }
    public string NombreUsuario { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public int RolId { get; init; }
    public string Rol { get; init; } = string.Empty;
    public bool IsActive { get; init; }
    public DateTime? UltimoAcceso { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime? UpdatedAt { get; init; }
    public string? CreatedBy { get; init; }
    public string? UpdatedBy { get; init; }
}
