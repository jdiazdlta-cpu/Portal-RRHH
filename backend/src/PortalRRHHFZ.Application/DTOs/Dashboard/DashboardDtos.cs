namespace PortalRRHHFZ.Application.DTOs.Dashboard;

public sealed class DashboardResumenDto
{
    public int TotalColaboradores { get; init; }
    public int TotalActivos { get; init; }
    public int TotalCesantes { get; init; }
    public int TotalVacaciones { get; init; }
    public int TotalServicio { get; init; }
    public int TotalSuspendidos { get; init; }
    public int TotalEmpresasActivas { get; init; }
    public int TotalDepartamentosActivos { get; init; }
    public int TotalCargosActivos { get; init; }
    public int TotalDocumentosActivos { get; init; }
    public int TotalAlertasActivas { get; init; }
    public int TotalAlertasPendientes { get; init; }
    public int TotalAlertasVencidas { get; init; }
    public int TotalAlertasGestionadas { get; init; }
    public int TotalAlertasIgnoradas { get; init; }
}

public sealed class DashboardVencimientosDto
{
    public int CedulasPorVencer { get; init; }
    public int LicenciasPorVencer { get; init; }
    public int ContratosPorVencer { get; init; }
    public int PeriodosProbatoriosPorVencer { get; init; }
    public int DocumentosPorVencer { get; init; }
    public int CedulasVencidas { get; init; }
    public int LicenciasVencidas { get; init; }
    public int ContratosVencidos { get; init; }
    public int PeriodosProbatoriosVencidos { get; init; }
    public int DocumentosVencidos { get; init; }
    public bool RequiereRecalculo { get; init; }
}

public sealed class ColaboradoresPorEstatusDto
{
    public int EstatusId { get; init; }
    public string Nombre { get; init; } = string.Empty;
    public string Codigo { get; init; } = string.Empty;
    public int Total { get; init; }
}

public sealed class ColaboradoresPorDepartamentoDto
{
    public int EmpresaId { get; init; }
    public string EmpresaNombre { get; init; } = string.Empty;
    public int DepartamentoId { get; init; }
    public string DepartamentoNombre { get; init; } = string.Empty;
    public int Total { get; init; }
}

public sealed class AltasBajasFilterRequest
{
    public int? Anio { get; init; }
    public int? EmpresaId { get; init; }
    public int? DepartamentoId { get; init; }
}

public sealed class AltasBajasDto
{
    public int Mes { get; init; }
    public int Altas { get; init; }
    public int Bajas { get; init; }
}

public sealed class MovimientoColaboradorDto
{
    public int ColaboradorId { get; init; }
    public string NombreCompleto { get; init; } = string.Empty;
    public string EmpresaNombre { get; init; } = string.Empty;
    public string DepartamentoNombre { get; init; } = string.Empty;
    public string CargoNombre { get; init; } = string.Empty;
    public DateTime? FechaIngreso { get; init; }
    public DateTime? FechaSalida { get; init; }
    public string EstatusNombre { get; init; } = string.Empty;
}

public sealed class UltimosMovimientosDto
{
    public IReadOnlyCollection<MovimientoColaboradorDto> UltimosIngresos { get; init; } = [];
    public IReadOnlyCollection<MovimientoColaboradorDto> UltimasSalidas { get; init; } = [];
}
