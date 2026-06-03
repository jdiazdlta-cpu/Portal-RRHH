namespace PortalRRHHFZ.Domain.Entities;

public sealed class Cargo : AuditableEntity
{
    public int CargoId { get; set; }
    public int DepartamentoId { get; set; }
    public string Nombre { get; set; } = string.Empty;

    public Departamento Departamento { get; set; } = null!;
    public ICollection<Colaborador> Colaboradores { get; set; } = [];
}
