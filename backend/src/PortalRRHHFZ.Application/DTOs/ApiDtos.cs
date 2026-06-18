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
