using PortalRRHHFZ.Application.Common;
using PortalRRHHFZ.Application.DTOs.Dashboard;

namespace PortalRRHHFZ.Application.Interfaces.Dashboard;

public interface IDashboardService
{
    Task<ApiResponse<DashboardResumenDto>> GetResumenAsync(
        CancellationToken cancellationToken = default);

    Task<ApiResponse<DashboardVencimientosDto>> GetVencimientosAsync(
        CancellationToken cancellationToken = default);

    Task<ApiResponse<IReadOnlyCollection<ColaboradoresPorEstatusDto>>> GetColaboradoresPorEstatusAsync(
        CancellationToken cancellationToken = default);

    Task<ApiResponse<IReadOnlyCollection<ColaboradoresPorDepartamentoDto>>> GetColaboradoresPorDepartamentoAsync(
        CancellationToken cancellationToken = default);

    Task<ApiResponse<IReadOnlyCollection<AltasBajasDto>>> GetAltasBajasAsync(
        AltasBajasFilterRequest filters,
        CancellationToken cancellationToken = default);

    Task<ApiResponse<UltimosMovimientosDto>> GetUltimosMovimientosAsync(
        CancellationToken cancellationToken = default);
}
