namespace PortalRRHHFZ.Application.DTOs;

public sealed class LoginRequest
{
    public string NombreUsuario { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

public sealed record CurrentUserDto(int UsuarioId, string NombreUsuario, string Email, string Rol);
public sealed record AuthResultDto(string Token, DateTime ExpiresAt, CurrentUserDto Usuario);

public sealed record RolDto(int RolId, string Nombre, string? Descripcion);
public sealed record CatalogoItemDto(int Id, string Nombre, string? Codigo = null, bool? RequiereFechaVencimiento = null, bool? TieneVencimientoSugerido = null);

public sealed record UsuarioDto(int UsuarioId, string NombreUsuario, string Email, int RolId, string Rol, DateTime? UltimoAcceso, bool IsActive);

public sealed class CreateUsuarioRequest
{
    public string NombreUsuario { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string? ConfirmPassword { get; set; }
    public int RolId { get; set; }
    public bool IsActive { get; set; } = true;
}

public sealed class UpdateUsuarioRequest
{
    public string NombreUsuario { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public int RolId { get; set; }
    public bool IsActive { get; set; } = true;
}

public sealed class ResetPasswordRequest
{
    public string Password { get; set; } = string.Empty;
}

public sealed record EmpresaDto(int EmpresaId, string Nombre, string? Ruc, bool IsActive);
public sealed class UpsertEmpresaRequest
{
    public string Nombre { get; set; } = string.Empty;
    public string? Ruc { get; set; }
}

public sealed record DepartamentoDto(int DepartamentoId, int EmpresaId, string Empresa, string Nombre, bool IsActive);
public sealed class UpsertDepartamentoRequest
{
    public int EmpresaId { get; set; }
    public string Nombre { get; set; } = string.Empty;
}

public sealed record CargoDto(int CargoId, int DepartamentoId, string Departamento, int EmpresaId, string Empresa, string Nombre, bool IsActive);
public sealed class UpsertCargoRequest
{
    public int DepartamentoId { get; set; }
    public string Nombre { get; set; } = string.Empty;
}

public sealed record ColaboradorListDto(
    int ColaboradorId,
    string NoEmpleado,
    string Cedula,
    string NombreCompleto,
    string Empresa,
    string Departamento,
    string Cargo,
    string Estatus,
    DateTime FechaIngreso,
    DateTime? FechaSalida,
    bool IsActive);

public sealed record PosibleJefeDto(
    int ColaboradorId,
    string NoEmpleado,
    string NombreCompleto,
    string Empresa,
    string Departamento,
    string Cargo);

public sealed record DocumentoDto(
    int DocumentoColaboradorId,
    int TipoDocumentoId,
    string TipoDocumento,
    int ColaboradorId,
    string NombreArchivo,
    string RutaArchivo,
    DateTime FechaCarga,
    bool TieneVencimiento,
    DateTime? FechaVencimiento,
    string? Observacion,
    bool IsActive);

public sealed record AlertaDto(
    int AlertaId,
    string TipoAlerta,
    string EstadoAlerta,
    int ColaboradorId,
    string Colaborador,
    int? DocumentoColaboradorId,
    DateTime FechaVencimiento,
    string Mensaje,
    DateTime FechaGeneracion,
    DateTime? FechaGestion,
    string? ObservacionGestion,
    string Empresa = "",
    int DiasRestantes = 0,
    int DiasVencidos = 0);

public sealed record RecordatorioDocumentoDto(
    int AlertaId,
    int ColaboradorId,
    string Colaborador,
    string Empresa,
    string TipoVencimiento,
    DateTime FechaVencimiento,
    int DiasRestantes);

public sealed record HistorialDto(
    int HistorialColaboradorId,
    string Usuario,
    string Accion,
    string? Campo,
    string? ValorAnterior,
    string? ValorNuevo,
    DateTime Fecha,
    string? Observacion);

public sealed record TipoSolicitudDisponibleDto(string Tipo, string Nombre, bool Disponible, string Estado);

public sealed record SolicitudListDto(
    int SolicitudId,
    string CodigoSolicitud,
    string TipoSolicitud,
    string Estado,
    string Solicitante,
    string? Empresa,
    string? Departamento,
    DateTime FechaSolicitud,
    DateTime? UltimaActualizacion);

public sealed record SolicitudDetailDto(
    int SolicitudId,
    string CodigoSolicitud,
    string TipoSolicitud,
    string Estado,
    int SolicitanteUsuarioId,
    string Solicitante,
    int? ColaboradorId,
    int? EmpresaId,
    string? Empresa,
    int? DepartamentoId,
    string? Departamento,
    int? CargoId,
    string? Cargo,
    DateTime FechaSolicitud,
    DateTime? FechaEfectiva,
    string? Justificacion,
    string? Observaciones,
    DateTime CreatedAt,
    DateTime? UpdatedAt,
    RequisicionPersonalDto? Requisicion,
    IReadOnlyList<SolicitudAprobacionDto> Aprobaciones,
    IReadOnlyList<SolicitudHistorialDto> Historial,
    IReadOnlyList<string> AccionesDisponibles);

public sealed record RequisicionPersonalDto(
    int RequisicionPersonalId,
    int SolicitudId,
    string CargoSolicitado,
    int? DepartamentoSolicitadoId,
    string? DepartamentoSolicitado,
    int NumeroPlazas,
    string? DependenciaJerarquica,
    string? PrincipalesResponsabilidades,
    string? FuncionesEspecificas,
    string? EquipoACargo,
    string? CentroTrabajo,
    decimal? Salario,
    decimal? GastoRepresentacion,
    decimal? SalarioVariable,
    string? OtrosConceptos,
    bool EsPosicionNueva,
    bool EsReemplazo,
    int? ColaboradorReemplazadoId,
    string? ColaboradorReemplazado,
    string? NombrePersonaReemplazada,
    int? TipoContratoId,
    string? TipoContrato,
    string? PeriodoPrueba,
    string? FormacionRequerida,
    string? FormacionComplementaria,
    string? ConocimientosTecnicos,
    string? ConocimientosValorados,
    string? IdiomaNivel,
    int? AniosExperiencia,
    string? FuncionesExperiencia,
    string? AreaSectorExperiencia,
    string? ExperienciaValorable,
    int? EdadMinima,
    int? EdadMaxima,
    string? SexoPreferido,
    string? CaracteristicasPersonales,
    DateTime? FechaAperturaProceso,
    DateTime? FechaEntregaCandidatos,
    string? SolicitadoPorTexto,
    string? AutorizadoPorTexto,
    DateTime? FechaAutorizacion);

public sealed record SolicitudAprobacionDto(
    int SolicitudAprobacionId,
    int Orden,
    string Etapa,
    string RolAprobador,
    int? UsuarioAprobadorId,
    string? UsuarioAprobador,
    int? ColaboradorAprobadorId,
    string? ColaboradorAprobador,
    int? DepartamentoResponsableId,
    string? TipoResponsable,
    string Estado,
    DateTime? FechaDecision,
    string? Comentario);

public sealed record OrganigramaListDto(
    int OrganigramaId,
    string Nombre,
    int? EmpresaId,
    string? Empresa,
    string? Descripcion,
    DateTime FechaInicio,
    DateTime? FechaFin,
    int Nodos,
    bool IsActive);

public sealed record OrganigramaDetailDto(
    int OrganigramaId,
    string Nombre,
    int? EmpresaId,
    string? Empresa,
    string? Descripcion,
    DateTime FechaInicio,
    DateTime? FechaFin,
    bool IsActive,
    IReadOnlyList<OrganigramaNodoDto> Nodos,
    IReadOnlyList<OrganigramaHistorialCambioDto> Historial);

public sealed record OrganigramaNodoDto(
    int OrganigramaNodoId,
    int OrganigramaId,
    int? NodoPadreId,
    string? NodoPadre,
    string NombreNodo,
    int? EmpresaId,
    string? Empresa,
    int? DepartamentoId,
    string? Departamento,
    int? CargoId,
    string? Cargo,
    int Nivel,
    int Orden,
    bool EsRolOperativo,
    int ColaboradoresActivos,
    string? Descripcion,
    bool IsActive);

public sealed record DepartamentoResponsableDto(
    int DepartamentoResponsableId,
    int EmpresaId,
    string Empresa,
    int DepartamentoId,
    string Departamento,
    int ColaboradorResponsableId,
    string NoEmpleado,
    string ColaboradorResponsable,
    int? UsuarioResponsableId,
    string? UsuarioResponsable,
    string TipoResponsable,
    bool EsPrincipal,
    bool PuedeAprobarSolicitudes,
    DateTime FechaInicio,
    DateTime? FechaFin,
    string? Observacion,
    bool IsActive,
    IReadOnlyList<string> Advertencias);

public sealed record AprobadorSolicitudDto(
    int DepartamentoResponsableId,
    int EmpresaId,
    string Empresa,
    int DepartamentoId,
    string Departamento,
    int ColaboradorResponsableId,
    string NoEmpleado,
    string NombreCompleto,
    string Cargo,
    string TipoResponsable,
    int? UsuarioResponsableId,
    string? UsuarioResponsable,
    bool EsPrincipal,
    IReadOnlyList<string> Advertencias);

public sealed record ColaboradorLookupDto(
    int ColaboradorId,
    string NoEmpleado,
    string NombreCompleto,
    int EmpresaId,
    string Empresa,
    int DepartamentoId,
    string Departamento,
    int CargoId,
    string Cargo,
    string Estatus);

public sealed record OrganigramaHistorialCambioDto(
    int OrganigramaHistorialCambioId,
    string Entidad,
    int EntidadId,
    string Accion,
    string? ValorAnterior,
    string? ValorNuevo,
    int UsuarioId,
    string Usuario,
    DateTime Fecha,
    string? Comentario);

public sealed record SolicitudHistorialDto(
    int SolicitudHistorialId,
    string Accion,
    string? EstadoAnterior,
    string? EstadoNuevo,
    string? Comentario,
    int UsuarioId,
    string Usuario,
    DateTime Fecha);

public class RequisicionPersonalRequestBase
{
    public int? EmpresaId { get; set; }
    public int? DepartamentoSolicitadoId { get; set; }
    public DateTime? FechaEfectiva { get; set; }
    public string? Justificacion { get; set; }
    public string? Observaciones { get; set; }
    public int? LiderAprobadorUsuarioId { get; set; }
    public int? LiderAprobadorColaboradorId { get; set; }
    public int? DepartamentoResponsableId { get; set; }
    public string CargoSolicitado { get; set; } = string.Empty;
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
}

public sealed class CreateRequisicionPersonalRequest : RequisicionPersonalRequestBase
{
    public bool Enviar { get; set; }
}

public sealed class UpdateRequisicionPersonalRequest : RequisicionPersonalRequestBase
{
}

public sealed class EnviarSolicitudRequest
{
    public string? Comentario { get; set; }
}

public sealed class DecidirSolicitudRequest
{
    public string? Comentario { get; set; }
}

public class CreateOrganigramaRequest
{
    public string Nombre { get; set; } = string.Empty;
    public int? EmpresaId { get; set; }
    public string? Descripcion { get; set; }
    public DateTime FechaInicio { get; set; } = DateTime.Today;
    public DateTime? FechaFin { get; set; }
    public bool IsActive { get; set; } = true;
}

public sealed class UpdateOrganigramaRequest : CreateOrganigramaRequest
{
}

public class CreateOrganigramaNodoRequest
{
    public int? EmpresaId { get; set; }
    public int? DepartamentoId { get; set; }
    public int? CargoId { get; set; }
    public int? NodoPadreId { get; set; }
    public string NombreNodo { get; set; } = string.Empty;
    public string? Descripcion { get; set; }
    public int Nivel { get; set; }
    public int Orden { get; set; }
    public bool EsRolOperativo { get; set; }
    public bool IsActive { get; set; } = true;
}

public sealed class UpdateOrganigramaNodoRequest : CreateOrganigramaNodoRequest
{
}

public class CreateDepartamentoResponsableRequest
{
    public int EmpresaId { get; set; }
    public int DepartamentoId { get; set; }
    public int ColaboradorResponsableId { get; set; }
    public int? UsuarioResponsableId { get; set; }
    public string TipoResponsable { get; set; } = string.Empty;
    public bool EsPrincipal { get; set; }
    public bool PuedeAprobarSolicitudes { get; set; } = true;
    public DateTime FechaInicio { get; set; } = DateTime.Today;
    public DateTime? FechaFin { get; set; }
    public string? Observacion { get; set; }
    public bool IsActive { get; set; } = true;
}

public sealed class UpdateDepartamentoResponsableRequest : CreateDepartamentoResponsableRequest
{
}

public sealed record ColaboradorDetalleDto(
    int ColaboradorId,
    string NoEmpleado,
    string Cedula,
    DateTime? FechaVencimientoCedula,
    string? SeguroSocial,
    string PrimerNombre,
    string? SegundoNombre,
    string PrimerApellido,
    string? SegundoApellido,
    string NombreCompleto,
    string? Sexo,
    string? Telefono,
    string? Email,
    DateTime? FechaNacimiento,
    string? Direccion,
    int EmpresaId,
    string Empresa,
    int DepartamentoId,
    string Departamento,
    int CargoId,
    string Cargo,
    int? JefeInmediatoId,
    string? JefeInmediato,
    DateTime FechaIngreso,
    int TipoContratoId,
    string TipoContrato,
    DateTime? FechaVencimientoContrato,
    DateTime? FechaVencimientoPeriodoProbatorio,
    bool TieneLicencia,
    string? NumeroLicencia,
    string? TipoLicencia,
    DateTime? FechaVencimientoLicencia,
    int EstatusId,
    string Estatus,
    decimal Salario,
    decimal Viaticos,
    decimal GastosRepresentacion,
    DateTime? FechaSalida,
    int? MotivoSalidaId,
    string? MotivoSalida,
    bool Vacante,
    DateTime? UltimaVacacion,
    bool IsActive,
    IReadOnlyList<DocumentoDto> Documentos,
    IReadOnlyList<AlertaDto> Alertas);

public sealed class UpsertColaboradorRequest
{
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
    public DateTime FechaIngreso { get; set; } = DateTime.Today;
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
}

public sealed class UpdateDocumentoRequest
{
    public int TipoDocumentoId { get; set; }
    public bool TieneVencimiento { get; set; }
    public DateTime? FechaVencimiento { get; set; }
    public string? Observacion { get; set; }
}

public sealed class AlertaGestionRequest
{
    public string? ObservacionGestion { get; set; }
}

public sealed class AlertaGestionCorreccionRequest
{
    public string? ObservacionGestion { get; set; }
    public bool GestionarSinCambio { get; set; }
    public string? ResultadoGestionContrato { get; set; }
    public DateTime? FechaVencimientoCedula { get; set; }
    public bool? TieneLicencia { get; set; }
    public string? NumeroLicencia { get; set; }
    public string? TipoLicencia { get; set; }
    public DateTime? FechaVencimientoLicencia { get; set; }
    public int? TipoContratoId { get; set; }
    public DateTime? FechaVencimientoContrato { get; set; }
    public DateTime? NuevaFechaVencimientoContrato { get; set; }
    public int? EstatusId { get; set; }
    public int? MotivoSalidaId { get; set; }
    public DateTime? FechaSalida { get; set; }
    public DateTime? FechaVencimientoPeriodoProbatorio { get; set; }
    public DateTime? FechaVencimientoDocumento { get; set; }
    public string? ObservacionDocumento { get; set; }
}

public sealed record DashboardResumenDto(
    int TotalColaboradores,
    int Activos,
    int Cesantes,
    int Vacaciones,
    int Servicio,
    int AlertasActivas,
    int Vencimientos);

public sealed record ChartItemDto(string Label, int Value);
public sealed record AltasBajasDto(string Periodo, int Altas, int Bajas);
public sealed record MovimientoDto(int HistorialColaboradorId, string Colaborador, string Usuario, string Accion, DateTime Fecha, string? Observacion);
