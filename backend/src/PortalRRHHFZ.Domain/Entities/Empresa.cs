namespace PortalRRHHFZ.Domain.Entities;

public sealed class Empresa : AuditableEntity
{
    public int EmpresaId { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string? Ruc { get; set; }

    public ICollection<Departamento> Departamentos { get; set; } = [];
    public ICollection<Colaborador> Colaboradores { get; set; } = [];
}
