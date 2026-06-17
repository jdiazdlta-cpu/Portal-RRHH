using PortalRRHHFZ.Domain.Enums;

namespace PortalRRHHFZ.Domain.Entities;

public sealed class Rol : BaseEntity
{
    public int RolId { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string? Descripcion { get; set; }
    public ICollection<Usuario> Usuarios { get; set; } = new List<Usuario>();
}

public sealed class Usuario : BaseEntity
{
    public int UsuarioId { get; set; }
    public string NombreUsuario { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public int RolId { get; set; }
    public DateTime? UltimoAcceso { get; set; }
    public Rol Rol { get; set; } = null!;
    public ICollection<DocumentoColaborador> DocumentosSubidos { get; set; } = new List<DocumentoColaborador>();
    public ICollection<Alerta> AlertasGestionadas { get; set; } = new List<Alerta>();
    public ICollection<HistorialColaborador> Historiales { get; set; } = new List<HistorialColaborador>();
}

public sealed class Empresa : BaseEntity
{
    public int EmpresaId { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string? Ruc { get; set; }
    public ICollection<Departamento> Departamentos { get; set; } = new List<Departamento>();
    public ICollection<Colaborador> Colaboradores { get; set; } = new List<Colaborador>();
}

public sealed class Departamento : BaseEntity
{
    public int DepartamentoId { get; set; }
    public int EmpresaId { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public Empresa Empresa { get; set; } = null!;
    public ICollection<Cargo> Cargos { get; set; } = new List<Cargo>();
    public ICollection<Colaborador> Colaboradores { get; set; } = new List<Colaborador>();
}

public sealed class Cargo : BaseEntity
{
    public int CargoId { get; set; }
    public int DepartamentoId { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public Departamento Departamento { get; set; } = null!;
    public ICollection<Colaborador> Colaboradores { get; set; } = new List<Colaborador>();
}

public sealed class TipoContrato : BaseEntity
{
    public int TipoContratoId { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public bool RequiereFechaVencimiento { get; set; }
    public ICollection<Colaborador> Colaboradores { get; set; } = new List<Colaborador>();
}

public sealed class EstatusColaborador : BaseEntity
{
    public int EstatusId { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string Codigo { get; set; } = string.Empty;
    public ICollection<Colaborador> Colaboradores { get; set; } = new List<Colaborador>();
}

public sealed class MotivoSalida : BaseEntity
{
    public int MotivoSalidaId { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public ICollection<Colaborador> Colaboradores { get; set; } = new List<Colaborador>();
}

public sealed class TipoDocumento : BaseEntity
{
    public int TipoDocumentoId { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public bool TieneVencimientoSugerido { get; set; }
    public ICollection<DocumentoColaborador> Documentos { get; set; } = new List<DocumentoColaborador>();
}

public sealed class Colaborador : BaseEntity
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
    public decimal Salario { get; set; }
    public decimal Viaticos { get; set; }
    public decimal GastosRepresentacion { get; set; }
    public DateTime? FechaSalida { get; set; }
    public int? MotivoSalidaId { get; set; }
    public bool Vacante { get; set; }
    public DateTime? UltimaVacacion { get; set; }

    public Empresa Empresa { get; set; } = null!;
    public Departamento Departamento { get; set; } = null!;
    public Cargo Cargo { get; set; } = null!;
    public Colaborador? JefeInmediato { get; set; }
    public ICollection<Colaborador> Subordinados { get; set; } = new List<Colaborador>();
    public TipoContrato TipoContrato { get; set; } = null!;
    public EstatusColaborador Estatus { get; set; } = null!;
    public MotivoSalida? MotivoSalida { get; set; }
    public ICollection<DocumentoColaborador> Documentos { get; set; } = new List<DocumentoColaborador>();
    public ICollection<Alerta> Alertas { get; set; } = new List<Alerta>();
    public ICollection<HistorialColaborador> Historiales { get; set; } = new List<HistorialColaborador>();
}

public sealed class DocumentoColaborador : BaseEntity
{
    public int DocumentoColaboradorId { get; set; }
    public int TipoDocumentoId { get; set; }
    public int ColaboradorId { get; set; }
    public string NombreArchivo { get; set; } = string.Empty;
    public string RutaArchivo { get; set; } = string.Empty;
    public DateTime FechaCarga { get; set; } = DateTime.UtcNow;
    public DateTime? FechaVencimiento { get; set; }
    public bool TieneVencimiento { get; set; }
    public string? Observacion { get; set; }
    public int SubidoPor { get; set; }

    public TipoDocumento TipoDocumento { get; set; } = null!;
    public Colaborador Colaborador { get; set; } = null!;
    public Usuario UsuarioSubio { get; set; } = null!;
    public ICollection<Alerta> Alertas { get; set; } = new List<Alerta>();
}

public sealed class Alerta : BaseEntity
{
    public int AlertaId { get; set; }
    public TipoAlerta TipoAlerta { get; set; }
    public EstadoAlerta EstadoAlerta { get; set; }
    public int ColaboradorId { get; set; }
    public int? DocumentoColaboradorId { get; set; }
    public DateTime FechaVencimiento { get; set; }
    public string Mensaje { get; set; } = string.Empty;
    public DateTime FechaGeneracion { get; set; } = DateTime.UtcNow;
    public DateTime? FechaGestion { get; set; }
    public int? GestionadaPor { get; set; }
    public string? ObservacionGestion { get; set; }

    public Colaborador Colaborador { get; set; } = null!;
    public DocumentoColaborador? DocumentoColaborador { get; set; }
    public Usuario? UsuarioGestiono { get; set; }
}

public sealed class HistorialColaborador : BaseEntity
{
    public int HistorialColaboradorId { get; set; }
    public int ColaboradorId { get; set; }
    public int UsuarioId { get; set; }
    public string Accion { get; set; } = string.Empty;
    public string? Campo { get; set; }
    public string? ValorAnterior { get; set; }
    public string? ValorNuevo { get; set; }
    public DateTime Fecha { get; set; } = DateTime.UtcNow;
    public string? Observacion { get; set; }

    public Colaborador Colaborador { get; set; } = null!;
    public Usuario Usuario { get; set; } = null!;
}
