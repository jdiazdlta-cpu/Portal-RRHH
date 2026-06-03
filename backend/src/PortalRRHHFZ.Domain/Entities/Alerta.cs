using PortalRRHHFZ.Domain.Enums;

namespace PortalRRHHFZ.Domain.Entities;

public sealed class Alerta : AuditableEntity
{
    public int AlertaId { get; set; }
    public TipoAlerta TipoAlerta { get; set; }
    public EstadoAlerta EstadoAlerta { get; set; } = EstadoAlerta.Pendiente;
    public int ColaboradorId { get; set; }
    public int? DocumentoColaboradorId { get; set; }
    public DateTime FechaVencimiento { get; set; }
    public string Mensaje { get; set; } = string.Empty;
    public DateTime FechaGeneracion { get; set; }
    public DateTime? FechaGestion { get; set; }
    public int? GestionadaPor { get; set; }
    public string? ObservacionGestion { get; set; }

    public Colaborador Colaborador { get; set; } = null!;
    public DocumentoColaborador? DocumentoColaborador { get; set; }
    public Usuario? UsuarioGestion { get; set; }
}
