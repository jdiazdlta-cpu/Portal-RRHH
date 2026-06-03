using System.ComponentModel.DataAnnotations;

namespace PortalRRHHFZ.Application.DTOs.Usuarios;

public sealed class ResetPasswordRequest
{
    [Required]
    public string Password { get; init; } = string.Empty;
}
