namespace PortalRRHHFZ.Domain.Entities;

public sealed class Departamento : AuditableEntity
{
    public int DepartamentoId { get; set; }
    public int EmpresaId { get; set; }
    public string Nombre { get; set; } = string.Empty;

    public Empresa Empresa { get; set; } = null!;
    public ICollection<Cargo> Cargos { get; set; } = [];
    public ICollection<Colaborador> Colaboradores { get; set; } = [];
}
