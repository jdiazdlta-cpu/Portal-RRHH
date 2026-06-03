using PortalRRHHFZ.Domain.Enums;

namespace PortalRRHHFZ.Application.DTOs.Alertas;

public sealed class AlertaFilterRequest
{
    public EstadoAlerta? EstadoAlerta { get; init; }
    public TipoAlerta? TipoAlerta { get; init; }
    public int? ColaboradorId { get; init; }
    public DateTime? Desde { get; init; }
    public DateTime? Hasta { get; init; }
    public bool IncluirInactivas { get; init; }
}

public sealed class AlertaListDto
{
    public int AlertaId { get; init; }
    public string TipoAlerta { get; init; } = string.Empty;
    public string EstadoAlerta { get; init; } = string.Empty;
    public int ColaboradorId { get; init; }
    public string NombreCompletoColaborador { get; init; } = string.Empty;
    public int? DocumentoColaboradorId { get; init; }
    public string? TipoDocumentoNombre { get; init; }
    public DateTime FechaVencimiento { get; init; }
    public string Mensaje { get; init; } = string.Empty;
    public DateTime FechaGeneracion { get; init; }
    public DateTime? FechaGestion { get; init; }
    public int? GestionadaPor { get; init; }
    public string? GestionadaPorNombre { get; init; }
    public string? ObservacionGestion { get; init; }
    public bool IsActive { get; init; }
}

public sealed class AlertaResumenDto
{
    public int TotalAlertas { get; init; }
    public int Pendientes { get; init; }
    public int Vencidas { get; init; }
    public int Gestionadas { get; init; }
    public int Ignoradas { get; init; }
    public IReadOnlyCollection<AlertaPorTipoDto> PorTipoAlerta { get; init; } = [];
    public int ProximasAVencer { get; init; }
    public int VencidasPendientes { get; init; }
}

public sealed class AlertaPorTipoDto
{
    public string TipoAlerta { get; init; } = string.Empty;
    public int Total { get; init; }
}

public sealed class GestionarAlertaRequest
{
    public string? ObservacionGestion { get; init; }
}

public sealed class RecalcularAlertasResultDto
{
    public int AlertasCreadas { get; init; }
    public int AlertasActualizadasAVencidas { get; init; }
    public int TotalAlertasActivas { get; init; }
}
