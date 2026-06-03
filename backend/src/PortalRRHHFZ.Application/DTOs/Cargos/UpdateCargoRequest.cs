using System.ComponentModel.DataAnnotations;

namespace PortalRRHHFZ.Application.DTOs.Cargos;

public sealed class UpdateCargoRequest
{
    [Required]
    public int DepartamentoId { get; init; }

    [Required]
    public string Nombre { get; init; } = string.Empty;
}
