namespace PortalRRHHFZ.Domain.Entities;

public sealed class TipoDocumento : AuditableEntity
{
    public int TipoDocumentoId { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public bool TieneVencimientoSugerido { get; set; }

    public ICollection<DocumentoColaborador> Documentos { get; set; } = [];
}
