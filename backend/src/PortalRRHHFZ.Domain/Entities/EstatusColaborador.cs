namespace PortalRRHHFZ.Domain.Entities;

public sealed class EstatusColaborador : AuditableEntity
{
    public int EstatusId { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string Codigo { get; set; } = string.Empty;

    public ICollection<Colaborador> Colaboradores { get; set; } = [];
}
