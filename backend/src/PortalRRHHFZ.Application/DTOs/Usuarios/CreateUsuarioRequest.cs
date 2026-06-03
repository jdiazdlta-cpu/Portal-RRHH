using System.ComponentModel.DataAnnotations;

namespace PortalRRHHFZ.Application.DTOs.Usuarios;

public sealed class CreateUsuarioRequest
{
    [Required]
    public string NombreUsuario { get; init; } = string.Empty;

    [Required]
    [EmailAddress]
    public string Email { get; init; } = string.Empty;

    [Required]
    public string Password { get; init; } = string.Empty;

    [Required]
    public int RolId { get; init; }

    public bool IsActive { get; init; } = true;
}
