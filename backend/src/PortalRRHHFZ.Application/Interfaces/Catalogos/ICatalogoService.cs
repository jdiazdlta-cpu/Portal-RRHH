using PortalRRHHFZ.Application.Common;
using PortalRRHHFZ.Application.DTOs.Catalogos;

namespace PortalRRHHFZ.Application.Interfaces.Catalogos;

public interface ICatalogoService
{
    Task<ApiResponse<IReadOnlyCollection<RolCatalogoDto>>> GetRolesAsync(CancellationToken cancellationToken = default);
    Task<ApiResponse<IReadOnlyCollection<EmpresaCatalogoDto>>> GetEmpresasAsync(CancellationToken cancellationToken = default);
    Task<ApiResponse<IReadOnlyCollection<DepartamentoCatalogoDto>>> GetDepartamentosAsync(int? empresaId, CancellationToken cancellationToken = default);
    Task<ApiResponse<IReadOnlyCollection<CargoCatalogoDto>>> GetCargosAsync(int? departamentoId, CancellationToken cancellationToken = default);
    Task<ApiResponse<IReadOnlyCollection<TipoContratoCatalogoDto>>> GetTiposContratoAsync(CancellationToken cancellationToken = default);
    Task<ApiResponse<IReadOnlyCollection<EstatusColaboradorCatalogoDto>>> GetEstatusColaboradorAsync(CancellationToken cancellationToken = default);
    Task<ApiResponse<IReadOnlyCollection<MotivoSalidaCatalogoDto>>> GetMotivosSalidaAsync(CancellationToken cancellationToken = default);
    Task<ApiResponse<IReadOnlyCollection<TipoDocumentoCatalogoDto>>> GetTiposDocumentoAsync(CancellationToken cancellationToken = default);
}
