namespace PortalRRHHFZ.Application.DTOs.Empresas;

public sealed class EmpresaDetailDto
{
    public int EmpresaId { get; init; }
    public string Nombre { get; init; } = string.Empty;
    public string? Ruc { get; init; }
    public bool IsActive { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime? UpdatedAt { get; init; }
    public string? CreatedBy { get; init; }
    public string? UpdatedBy { get; init; }
}
