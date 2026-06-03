namespace PortalRRHHFZ.Application.DTOs.Departamentos;

public sealed class DepartamentoDetailDto
{
    public int DepartamentoId { get; init; }
    public int EmpresaId { get; init; }
    public string EmpresaNombre { get; init; } = string.Empty;
    public string Nombre { get; init; } = string.Empty;
    public bool IsActive { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime? UpdatedAt { get; init; }
    public string? CreatedBy { get; init; }
    public string? UpdatedBy { get; init; }
}
