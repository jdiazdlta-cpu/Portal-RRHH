namespace PortalRRHHFZ.Application.DTOs.Catalogos;

public sealed class TipoContratoCatalogoDto
{
    public int TipoContratoId { get; init; }
    public string Nombre { get; init; } = string.Empty;
    public bool RequiereFechaVencimiento { get; init; }
}
