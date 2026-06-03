namespace PortalRRHHFZ.Domain.Entities;

public sealed class TipoContrato : AuditableEntity
{
    public int TipoContratoId { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public bool RequiereFechaVencimiento { get; set; }

    public ICollection<Colaborador> Colaboradores { get; set; } = [];
}
