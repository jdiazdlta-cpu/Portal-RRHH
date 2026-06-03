using System.ComponentModel.DataAnnotations;

namespace PortalRRHHFZ.Application.DTOs.Departamentos;

public sealed class UpdateDepartamentoRequest
{
    [Required]
    public int EmpresaId { get; init; }

    [Required]
    public string Nombre { get; init; } = string.Empty;
}
