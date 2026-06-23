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

public sealed class Solicitud : BaseEntity
{
    public int SolicitudId { get; set; }
    public string CodigoSolicitud { get; set; } = string.Empty;
    public TipoSolicitud TipoSolicitud { get; set; }
    public EstadoSolicitud Estado { get; set; } = EstadoSolicitud.Borrador;
    public int SolicitanteUsuarioId { get; set; }
    public int? ColaboradorId { get; set; }
    public int? EmpresaId { get; set; }
    public int? DepartamentoId { get; set; }
    public int? CargoId { get; set; }
    public DateTime FechaSolicitud { get; set; } = DateTime.UtcNow;
    public DateTime? FechaEfectiva { get; set; }
    public string? Justificacion { get; set; }
    public string? Observaciones { get; set; }

    public Usuario SolicitanteUsuario { get; set; } = null!;
    public Colaborador? Colaborador { get; set; }
    public Empresa? Empresa { get; set; }
    public Departamento? Departamento { get; set; }
    public Cargo? Cargo { get; set; }
    public RequisicionPersonal? RequisicionPersonal { get; set; }
    public AccionPersonal? AccionPersonal { get; set; }
    public ICollection<SolicitudAprobacion> Aprobaciones { get; set; } = new List<SolicitudAprobacion>();
    public ICollection<SolicitudHistorial> Historial { get; set; } = new List<SolicitudHistorial>();
}

public sealed class RequisicionPersonal : BaseEntity
{
    public int RequisicionPersonalId { get; set; }
    public int SolicitudId { get; set; }
    public string CargoSolicitado { get; set; } = string.Empty;
    public int? DepartamentoSolicitadoId { get; set; }
    public int NumeroPlazas { get; set; } = 1;
    public string? DependenciaJerarquica { get; set; }
    public string? PrincipalesResponsabilidades { get; set; }
    public string? FuncionesEspecificas { get; set; }
    public string? EquipoACargo { get; set; }
    public string? CentroTrabajo { get; set; }
    public decimal? Salario { get; set; }
    public decimal? GastoRepresentacion { get; set; }
    public decimal? SalarioVariable { get; set; }
    public string? OtrosConceptos { get; set; }
    public bool EsPosicionNueva { get; set; }
    public bool EsReemplazo { get; set; }
    public int? ColaboradorReemplazadoId { get; set; }
    public string? NombrePersonaReemplazada { get; set; }
    public int? TipoContratoId { get; set; }
    public string? PeriodoPrueba { get; set; }
    public string? FormacionRequerida { get; set; }
    public string? FormacionComplementaria { get; set; }
    public string? ConocimientosTecnicos { get; set; }
    public string? ConocimientosValorados { get; set; }
    public string? IdiomaNivel { get; set; }
    public int? AniosExperiencia { get; set; }
    public string? FuncionesExperiencia { get; set; }
    public string? AreaSectorExperiencia { get; set; }
    public string? ExperienciaValorable { get; set; }
    public int? EdadMinima { get; set; }
    public int? EdadMaxima { get; set; }
    public string? SexoPreferido { get; set; }
    public string? CaracteristicasPersonales { get; set; }
    public DateTime? FechaAperturaProceso { get; set; }
    public DateTime? FechaEntregaCandidatos { get; set; }
    public string? SolicitadoPorTexto { get; set; }
    public string? AutorizadoPorTexto { get; set; }
    public DateTime? FechaAutorizacion { get; set; }

    public Solicitud Solicitud { get; set; } = null!;
    public Departamento? DepartamentoSolicitado { get; set; }
    public Colaborador? ColaboradorReemplazado { get; set; }
    public TipoContrato? TipoContrato { get; set; }
}

public sealed class AccionPersonal : BaseEntity
{
    public int AccionPersonalId { get; set; }
    public int SolicitudId { get; set; }
    public TipoAccionPersonal TipoAccion { get; set; }
    public int? ColaboradorId { get; set; }
    public DateTime FechaEfectiva { get; set; }
    public string Justificacion { get; set; } = string.Empty;
    public string? Observaciones { get; set; }

    public string? NombreColaboradorSnapshot { get; set; }
    public string? NoEmpleadoSnapshot { get; set; }
    public string? CedulaSnapshot { get; set; }
    public int? EmpresaActualId { get; set; }
    public int? DepartamentoActualId { get; set; }
    public int? CargoActualId { get; set; }
    public int? JefeActualId { get; set; }
    public int? TipoContratoActualId { get; set; }
    public int? EstatusActualId { get; set; }
    public decimal? SalarioActual { get; set; }
    public decimal? ViaticosActual { get; set; }
    public decimal? GastosRepresentacionActual { get; set; }

    public int? DiasVacaciones { get; set; }
    public DateTime? FechaInicioVacaciones { get; set; }
    public DateTime? FechaFinVacaciones { get; set; }
    public DateTime? PeriodoVacacionesDesde { get; set; }
    public DateTime? PeriodoVacacionesHasta { get; set; }
    public string? QuienReemplaza { get; set; }

    public int? TipoContratoNuevoId { get; set; }
    public DateTime? FechaInicioContrato { get; set; }
    public DateTime? FechaFinContrato { get; set; }
    public bool? EsReemplazo { get; set; }
    public bool? EsPosicionNueva { get; set; }
    public decimal? SalarioNuevo { get; set; }
    public decimal? ViaticosNuevo { get; set; }
    public decimal? GastosRepresentacionNuevo { get; set; }
    public string? OtrosBeneficios { get; set; }

    public decimal? SalarioAnterior { get; set; }
    public decimal? SalarioNuevoAjuste { get; set; }
    public decimal? AjustePorMes { get; set; }
    public string? MotivoAjuste { get; set; }

    public int? CargoNuevoId { get; set; }
    public int? DepartamentoNuevoId { get; set; }
    public int? EmpresaNuevaId { get; set; }
    public int? JefeNuevoId { get; set; }

    public int? CargoTrasladoActualId { get; set; }
    public int? CargoTrasladoNuevoId { get; set; }
    public int? DepartamentoTrasladoActualId { get; set; }
    public int? DepartamentoTrasladoNuevoId { get; set; }
    public int? EmpresaTrasladoActualId { get; set; }
    public int? EmpresaTrasladoNuevaId { get; set; }
    public int? JefeTrasladoNuevoId { get; set; }

    public string? TipoLicenciaAccion { get; set; }
    public bool? LicenciaRemunerada { get; set; }
    public DateTime? FechaInicioLicencia { get; set; }
    public DateTime? FechaFinLicencia { get; set; }
    public string? EspecificacionLicencia { get; set; }

    public string? TipoFinalizacion { get; set; }
    public DateTime? FechaSalida { get; set; }
    public int? MotivoSalidaId { get; set; }
    public bool? MenosDeDosAnios { get; set; }
    public bool? TerminacionPeriodoPrueba { get; set; }
    public bool? CausaJustificada { get; set; }
    public bool? MutuoAcuerdo { get; set; }
    public bool? RenovacionExtensionContrato { get; set; }
    public bool? ContinuidadLaboral { get; set; }
    public bool? LoRecomienda { get; set; }

    public string? Puntualidad { get; set; }
    public string? Honestidad { get; set; }
    public string? TrabajoEquipo { get; set; }
    public string? Productividad { get; set; }
    public string? Iniciativa { get; set; }
    public string? RespetoJefe { get; set; }
    public string? RespetoCompaneros { get; set; }

    public bool Ejecutada { get; set; }
    public DateTime? FechaEjecucion { get; set; }
    public int? EjecutadaPorUsuarioId { get; set; }
    public string? ResultadoEjecucion { get; set; }
    public string? ErrorEjecucion { get; set; }

    public Solicitud Solicitud { get; set; } = null!;
    public Colaborador? Colaborador { get; set; }
    public Empresa? EmpresaActual { get; set; }
    public Departamento? DepartamentoActual { get; set; }
    public Cargo? CargoActual { get; set; }
    public Colaborador? JefeActual { get; set; }
    public TipoContrato? TipoContratoActual { get; set; }
    public EstatusColaborador? EstatusActual { get; set; }
    public TipoContrato? TipoContratoNuevo { get; set; }
    public Cargo? CargoNuevo { get; set; }
    public Departamento? DepartamentoNuevo { get; set; }
    public Empresa? EmpresaNueva { get; set; }
    public Colaborador? JefeNuevo { get; set; }
    public Cargo? CargoTrasladoActual { get; set; }
    public Cargo? CargoTrasladoNuevo { get; set; }
    public Departamento? DepartamentoTrasladoActual { get; set; }
    public Departamento? DepartamentoTrasladoNuevo { get; set; }
    public Empresa? EmpresaTrasladoActual { get; set; }
    public Empresa? EmpresaTrasladoNueva { get; set; }
    public Colaborador? JefeTrasladoNuevo { get; set; }
    public MotivoSalida? MotivoSalida { get; set; }
    public Usuario? EjecutadaPorUsuario { get; set; }
    public ICollection<AccionPersonalCambioAplicado> CambiosAplicados { get; set; } = new List<AccionPersonalCambioAplicado>();
}

public sealed class AccionPersonalCambioAplicado : BaseEntity
{
    public int AccionPersonalCambioAplicadoId { get; set; }
    public int AccionPersonalId { get; set; }
    public string Campo { get; set; } = string.Empty;
    public string? ValorAnterior { get; set; }
    public string? ValorNuevo { get; set; }
    public DateTime Fecha { get; set; } = DateTime.UtcNow;
    public int UsuarioId { get; set; }

    public AccionPersonal AccionPersonal { get; set; } = null!;
    public Usuario Usuario { get; set; } = null!;
}

public sealed class SolicitudAprobacion : BaseEntity
{
    public int SolicitudAprobacionId { get; set; }
    public int SolicitudId { get; set; }
    public int Orden { get; set; }
    public EtapaAprobacion Etapa { get; set; }
    public string RolAprobador { get; set; } = string.Empty;
    public int? UsuarioAprobadorId { get; set; }
    public int? ColaboradorAprobadorId { get; set; }
    public int? DepartamentoResponsableId { get; set; }
    public EstadoAprobacion Estado { get; set; } = EstadoAprobacion.Pendiente;
    public DateTime? FechaDecision { get; set; }
    public string? Comentario { get; set; }

    public Solicitud Solicitud { get; set; } = null!;
    public Usuario? UsuarioAprobador { get; set; }
    public Colaborador? ColaboradorAprobador { get; set; }
    public DepartamentoResponsable? DepartamentoResponsable { get; set; }
}

public sealed class SolicitudHistorial
{
    public int SolicitudHistorialId { get; set; }
    public int SolicitudId { get; set; }
    public string Accion { get; set; } = string.Empty;
    public EstadoSolicitud? EstadoAnterior { get; set; }
    public EstadoSolicitud? EstadoNuevo { get; set; }
    public string? Comentario { get; set; }
    public int UsuarioId { get; set; }
    public DateTime Fecha { get; set; } = DateTime.UtcNow;

    public Solicitud Solicitud { get; set; } = null!;
    public Usuario Usuario { get; set; } = null!;
}

public sealed class Organigrama : BaseEntity
{
    public int OrganigramaId { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public int? EmpresaId { get; set; }
    public string? Descripcion { get; set; }
    public DateTime FechaInicio { get; set; }
    public DateTime? FechaFin { get; set; }

    public Empresa? Empresa { get; set; }
    public ICollection<OrganigramaNodo> Nodos { get; set; } = new List<OrganigramaNodo>();
}

public sealed class OrganigramaNodo : BaseEntity
{
    public int OrganigramaNodoId { get; set; }
    public int OrganigramaId { get; set; }
    public int? EmpresaId { get; set; }
    public int? DepartamentoId { get; set; }
    public int? CargoId { get; set; }
    public int? NodoPadreId { get; set; }
    public string NombreNodo { get; set; } = string.Empty;
    public string? Descripcion { get; set; }
    public int Nivel { get; set; }
    public int Orden { get; set; }
    public bool EsRolOperativo { get; set; }

    public Organigrama Organigrama { get; set; } = null!;
    public Empresa? Empresa { get; set; }
    public Departamento? Departamento { get; set; }
    public Cargo? Cargo { get; set; }
    public OrganigramaNodo? NodoPadre { get; set; }
    public ICollection<OrganigramaNodo> Hijos { get; set; } = new List<OrganigramaNodo>();
}

public sealed class DepartamentoResponsable : BaseEntity
{
    public int DepartamentoResponsableId { get; set; }
    public int EmpresaId { get; set; }
    public int DepartamentoId { get; set; }
    public int ColaboradorResponsableId { get; set; }
    public int? UsuarioResponsableId { get; set; }
    public string TipoResponsable { get; set; } = string.Empty;
    public bool EsPrincipal { get; set; }
    public bool PuedeAprobarSolicitudes { get; set; }
    public DateTime FechaInicio { get; set; }
    public DateTime? FechaFin { get; set; }
    public string? Observacion { get; set; }

    public Empresa Empresa { get; set; } = null!;
    public Departamento Departamento { get; set; } = null!;
    public Colaborador ColaboradorResponsable { get; set; } = null!;
    public Usuario? UsuarioResponsable { get; set; }
}

public sealed class OrganigramaHistorialCambio
{
    public int OrganigramaHistorialCambioId { get; set; }
    public string Entidad { get; set; } = string.Empty;
    public int EntidadId { get; set; }
    public string Accion { get; set; } = string.Empty;
    public string? ValorAnterior { get; set; }
    public string? ValorNuevo { get; set; }
    public int UsuarioId { get; set; }
    public DateTime Fecha { get; set; } = DateTime.UtcNow;
    public string? Comentario { get; set; }

    public Usuario Usuario { get; set; } = null!;
}
