namespace PortalRRHHFZ.Application.DTOs.Catalogos;

public sealed class TipoDocumentoCatalogoDto
{
    public int TipoDocumentoId { get; init; }
    public string Nombre { get; init; } = string.Empty;
    public bool TieneVencimientoSugerido { get; init; }
}
