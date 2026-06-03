namespace PortalRRHHFZ.Domain.Entities;

public sealed class Colaborador : AuditableEntity
{
    public int ColaboradorId { get; set; }
    public string NoEmpleado { get; set; } = string.Empty;
    public string Cedula { get; set; } = string.Empty;
    public DateTime? FechaVencimientoCedula { get; set; }
    public string? SeguroSocial { get; set; }
    public string PrimerNombre { get; set; } = string.Empty;
    public string? SegundoNombre { get; set; }
    public string PrimerApellido { get; set; } = string.Empty;
    public string? SegundoApellido { get; set; }
    public string? Sexo { get; set; }
    public string? Telefono { get; set; }
    public string? Email { get; set; }
    public DateTime? FechaNacimiento { get; set; }
    public string? Direccion { get; set; }
    public int EmpresaId { get; set; }
    public int DepartamentoId { get; set; }
    public int CargoId { get; set; }
    public int? JefeInmediatoId { get; set; }
    public DateTime FechaIngreso { get; set; }
    public int TipoContratoId { get; set; }
    public DateTime? FechaVencimientoContrato { get; set; }
    public DateTime? FechaVencimientoPeriodoProbatorio { get; set; }
    public bool TieneLicencia { get; set; }
    public string? NumeroLicencia { get; set; }
    public string? TipoLicencia { get; set; }
    public DateTime? FechaVencimientoLicencia { get; set; }
    public int EstatusId { get; set; }
    public decimal? Salario { get; set; }
    public decimal? Viaticos { get; set; }
    public decimal? GastosRepresentacion { get; set; }
    public DateTime? FechaSalida { get; set; }
    public int? MotivoSalidaId { get; set; }
    public bool Vacante { get; set; }
    public DateTime? UltimaVacacion { get; set; }

    public Empresa Empresa { get; set; } = null!;
    public Departamento Departamento { get; set; } = null!;
    public Cargo Cargo { get; set; } = null!;
    public Colaborador? JefeInmediato { get; set; }
    public TipoContrato TipoContrato { get; set; } = null!;
    public EstatusColaborador Estatus { get; set; } = null!;
    public MotivoSalida? MotivoSalida { get; set; }
    public ICollection<Colaborador> Subordinados { get; set; } = [];
    public ICollection<DocumentoColaborador> Documentos { get; set; } = [];
    public ICollection<Alerta> Alertas { get; set; } = [];
    public ICollection<HistorialColaborador> Historiales { get; set; } = [];
}
