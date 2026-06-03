namespace PortalRRHHFZ.Domain.Entities;

public sealed class DocumentoColaborador : AuditableEntity
{
    public int DocumentoColaboradorId { get; set; }
    public int TipoDocumentoId { get; set; }
    public int ColaboradorId { get; set; }
    public string NombreArchivo { get; set; } = string.Empty;
    public string RutaArchivo { get; set; } = string.Empty;
    public DateTime FechaCarga { get; set; }
    public DateTime? FechaVencimiento { get; set; }
    public bool TieneVencimiento { get; set; }
    public string? Observacion { get; set; }
    public int SubidoPor { get; set; }

    public TipoDocumento TipoDocumento { get; set; } = null!;
    public Colaborador Colaborador { get; set; } = null!;
    public Usuario UsuarioSubida { get; set; } = null!;
    public ICollection<Alerta> Alertas { get; set; } = [];
}
