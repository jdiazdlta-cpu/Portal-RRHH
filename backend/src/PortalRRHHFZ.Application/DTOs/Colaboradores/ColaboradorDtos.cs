namespace PortalRRHHFZ.Application.DTOs.Colaboradores;

public sealed class ColaboradorFilterRequest
{
    public int? EmpresaId { get; init; }
    public int? DepartamentoId { get; init; }
    public int? CargoId { get; init; }
    public int? EstatusId { get; init; }
    public int? TipoContratoId { get; init; }
    public string? Search { get; init; }
    public bool? IsActive { get; init; }
}

public sealed class ColaboradorListDto
{
    public int ColaboradorId { get; init; }
    public string NoEmpleado { get; init; } = string.Empty;
    public string Cedula { get; init; } = string.Empty;
    public string NombreCompleto { get; init; } = string.Empty;
    public string? Email { get; init; }
    public int EmpresaId { get; init; }
    public string EmpresaNombre { get; init; } = string.Empty;
    public int DepartamentoId { get; init; }
    public string DepartamentoNombre { get; init; } = string.Empty;
    public int CargoId { get; init; }
    public string CargoNombre { get; init; } = string.Empty;
    public int EstatusId { get; init; }
    public string EstatusNombre { get; init; } = string.Empty;
    public int TipoContratoId { get; init; }
    public string TipoContratoNombre { get; init; } = string.Empty;
    public DateTime FechaIngreso { get; init; }
    public bool IsActive { get; init; }
}

public sealed class ColaboradorDetailDto
{
    public int ColaboradorId { get; init; }
    public string NoEmpleado { get; init; } = string.Empty;
    public string Cedula { get; init; } = string.Empty;
    public DateTime? FechaVencimientoCedula { get; init; }
    public string? SeguroSocial { get; init; }
    public string PrimerNombre { get; init; } = string.Empty;
    public string? SegundoNombre { get; init; }
    public string PrimerApellido { get; init; } = string.Empty;
    public string? SegundoApellido { get; init; }
    public string NombreCompleto { get; init; } = string.Empty;
    public string? Sexo { get; init; }
    public string? Telefono { get; init; }
    public string? Email { get; init; }
    public DateTime? FechaNacimiento { get; init; }
    public string? Direccion { get; init; }
    public int EmpresaId { get; init; }
    public string EmpresaNombre { get; init; } = string.Empty;
    public int DepartamentoId { get; init; }
    public string DepartamentoNombre { get; init; } = string.Empty;
    public int CargoId { get; init; }
    public string CargoNombre { get; init; } = string.Empty;
    public int? JefeInmediatoId { get; init; }
    public string? JefeInmediatoNombre { get; init; }
    public DateTime FechaIngreso { get; init; }
    public int TipoContratoId { get; init; }
    public string TipoContratoNombre { get; init; } = string.Empty;
    public DateTime? FechaVencimientoContrato { get; init; }
    public DateTime? FechaVencimientoPeriodoProbatorio { get; init; }
    public bool TieneLicencia { get; init; }
    public string? NumeroLicencia { get; init; }
    public string? TipoLicencia { get; init; }
    public DateTime? FechaVencimientoLicencia { get; init; }
    public int EstatusId { get; init; }
    public string EstatusNombre { get; init; } = string.Empty;
    public decimal? Salario { get; init; }
    public decimal? Viaticos { get; init; }
    public decimal? GastosRepresentacion { get; init; }
    public DateTime? FechaSalida { get; init; }
    public int? MotivoSalidaId { get; init; }
    public string? MotivoSalidaNombre { get; init; }
    public bool Vacante { get; init; }
    public DateTime? UltimaVacacion { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime? UpdatedAt { get; init; }
    public string? CreatedBy { get; init; }
    public string? UpdatedBy { get; init; }
    public bool IsActive { get; init; }
}

public sealed class ColaboradorPerfilDto
{
    public ColaboradorPerfilDatosPersonalesDto DatosPersonales { get; init; } = new();
    public ColaboradorPerfilDatosLaboralesDto DatosLaborales { get; init; } = new();
    public ColaboradorPerfilContratoDto Contrato { get; init; } = new();
    public ColaboradorPerfilVencimientosDto Vencimientos { get; init; } = new();
    public ColaboradorPerfilCompensacionDto Compensacion { get; init; } = new();
}

public sealed class ColaboradorPerfilDatosPersonalesDto
{
    public int ColaboradorId { get; init; }
    public string NoEmpleado { get; init; } = string.Empty;
    public string Cedula { get; init; } = string.Empty;
    public string NombreCompleto { get; init; } = string.Empty;
    public string? Sexo { get; init; }
    public string? Telefono { get; init; }
    public string? Email { get; init; }
    public DateTime? FechaNacimiento { get; init; }
    public string? Direccion { get; init; }
}

public sealed class ColaboradorPerfilDatosLaboralesDto
{
    public int EmpresaId { get; init; }
    public string EmpresaNombre { get; init; } = string.Empty;
    public int DepartamentoId { get; init; }
    public string DepartamentoNombre { get; init; } = string.Empty;
    public int CargoId { get; init; }
    public string CargoNombre { get; init; } = string.Empty;
    public int? JefeInmediatoId { get; init; }
    public string? JefeInmediatoNombre { get; init; }
    public int EstatusId { get; init; }
    public string EstatusNombre { get; init; } = string.Empty;
    public DateTime FechaIngreso { get; init; }
    public DateTime? FechaSalida { get; init; }
    public int? MotivoSalidaId { get; init; }
    public string? MotivoSalidaNombre { get; init; }
    public bool Vacante { get; init; }
    public bool IsActive { get; init; }
}

public sealed class ColaboradorPerfilContratoDto
{
    public int TipoContratoId { get; init; }
    public string TipoContratoNombre { get; init; } = string.Empty;
    public DateTime? FechaVencimientoContrato { get; init; }
    public DateTime? FechaVencimientoPeriodoProbatorio { get; init; }
}

public sealed class ColaboradorPerfilVencimientosDto
{
    public DateTime? FechaVencimientoCedula { get; init; }
    public bool TieneLicencia { get; init; }
    public string? NumeroLicencia { get; init; }
    public string? TipoLicencia { get; init; }
    public DateTime? FechaVencimientoLicencia { get; init; }
    public DateTime? FechaVencimientoContrato { get; init; }
    public DateTime? FechaVencimientoPeriodoProbatorio { get; init; }
}

public sealed class ColaboradorPerfilCompensacionDto
{
    public decimal? Salario { get; init; }
    public decimal? Viaticos { get; init; }
    public decimal? GastosRepresentacion { get; init; }
}

public class CreateColaboradorRequest
{
    public string NoEmpleado { get; init; } = string.Empty;
    public string Cedula { get; init; } = string.Empty;
    public DateTime? FechaVencimientoCedula { get; init; }
    public string? SeguroSocial { get; init; }
    public string PrimerNombre { get; init; } = string.Empty;
    public string? SegundoNombre { get; init; }
    public string PrimerApellido { get; init; } = string.Empty;
    public string? SegundoApellido { get; init; }
    public string? Sexo { get; init; }
    public string? Telefono { get; init; }
    public string? Email { get; init; }
    public DateTime? FechaNacimiento { get; init; }
    public string? Direccion { get; init; }
    public int EmpresaId { get; init; }
    public int DepartamentoId { get; init; }
    public int CargoId { get; init; }
    public int? JefeInmediatoId { get; init; }
    public DateTime? FechaIngreso { get; init; }
    public int TipoContratoId { get; init; }
    public DateTime? FechaVencimientoContrato { get; init; }
    public DateTime? FechaVencimientoPeriodoProbatorio { get; init; }
    public bool TieneLicencia { get; init; }
    public string? NumeroLicencia { get; init; }
    public string? TipoLicencia { get; init; }
    public DateTime? FechaVencimientoLicencia { get; init; }
    public int EstatusId { get; init; }
    public decimal? Salario { get; init; }
    public decimal? Viaticos { get; init; }
    public decimal? GastosRepresentacion { get; init; }
    public DateTime? FechaSalida { get; init; }
    public int? MotivoSalidaId { get; init; }
    public bool Vacante { get; init; }
    public DateTime? UltimaVacacion { get; init; }
}

public sealed class UpdateColaboradorRequest : CreateColaboradorRequest;

public sealed class HistorialColaboradorDto
{
    public int HistorialColaboradorId { get; init; }
    public int ColaboradorId { get; init; }
    public int UsuarioId { get; init; }
    public string UsuarioNombre { get; init; } = string.Empty;
    public string Accion { get; init; } = string.Empty;
    public string? Campo { get; init; }
    public string? ValorAnterior { get; init; }
    public string? ValorNuevo { get; init; }
    public DateTime Fecha { get; init; }
    public string? Observacion { get; init; }
}
