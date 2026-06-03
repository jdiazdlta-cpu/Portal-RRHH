namespace PortalRRHHFZ.Domain.Entities;

public sealed class MotivoSalida : AuditableEntity
{
    public int MotivoSalidaId { get; set; }
    public string Nombre { get; set; } = string.Empty;

    public ICollection<Colaborador> Colaboradores { get; set; } = [];
}
