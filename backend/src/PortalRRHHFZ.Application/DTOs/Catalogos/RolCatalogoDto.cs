namespace PortalRRHHFZ.Application.DTOs.Catalogos;

public sealed class RolCatalogoDto
{
    public int RolId { get; init; }
    public string Nombre { get; init; } = string.Empty;
    public string? Descripcion { get; init; }
}
