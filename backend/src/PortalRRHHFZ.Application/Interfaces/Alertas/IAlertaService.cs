using System.Security.Claims;
using PortalRRHHFZ.Application.Common;
using PortalRRHHFZ.Application.DTOs.Alertas;

namespace PortalRRHHFZ.Application.Interfaces.Alertas;

public interface IAlertaService
{
    Task<ApiResponse<IReadOnlyCollection<AlertaListDto>>> GetAllAsync(
        AlertaFilterRequest filters,
        CancellationToken cancellationToken = default);

    Task<ApiResponse<AlertaResumenDto>> GetResumenAsync(
        CancellationToken cancellationToken = default);

    Task<ApiResponse<AlertaListDto>> GestionarAsync(
        int id,
        GestionarAlertaRequest request,
        ClaimsPrincipal principal,
        CancellationToken cancellationToken = default);

    Task<ApiResponse<AlertaListDto>> IgnorarAsync(
        int id,
        GestionarAlertaRequest request,
        ClaimsPrincipal principal,
        CancellationToken cancellationToken = default);

    Task<ApiResponse<RecalcularAlertasResultDto>> RecalcularAsync(
        ClaimsPrincipal principal,
        CancellationToken cancellationToken = default);
}
