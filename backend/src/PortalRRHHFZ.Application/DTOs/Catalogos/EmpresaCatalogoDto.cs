namespace PortalRRHHFZ.Application.DTOs.Catalogos;

public sealed class EmpresaCatalogoDto
{
    public int EmpresaId { get; init; }
    public string Nombre { get; init; } = string.Empty;
    public string? Ruc { get; init; }
}
