namespace PortalRRHHFZ.Application.DTOs.Empresas;

public sealed class EmpresaListDto
{
    public int EmpresaId { get; init; }
    public string Nombre { get; init; } = string.Empty;
    public string? Ruc { get; init; }
    public bool IsActive { get; init; }
    public DateTime CreatedAt { get; init; }
}
