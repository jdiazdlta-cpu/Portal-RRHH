namespace PortalRRHHFZ.Domain.Entities;

public sealed class HistorialColaborador : AuditableEntity
{
    public int HistorialColaboradorId { get; set; }
    public int ColaboradorId { get; set; }
    public int UsuarioId { get; set; }
    public string Accion { get; set; } = string.Empty;
    public string? Campo { get; set; }
    public string? ValorAnterior { get; set; }
    public string? ValorNuevo { get; set; }
    public DateTime Fecha { get; set; }
    public string? Observacion { get; set; }

    public Colaborador Colaborador { get; set; } = null!;
    public Usuario Usuario { get; set; } = null!;
}
