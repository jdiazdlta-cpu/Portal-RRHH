using System.ComponentModel.DataAnnotations;

namespace PortalRRHHFZ.Application.DTOs.Empresas;

public sealed class UpdateEmpresaRequest
{
    [Required]
    public string Nombre { get; init; } = string.Empty;

    public string? Ruc { get; init; }
}
