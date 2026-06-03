using System.Security.Claims;
using PortalRRHHFZ.Application.Common;
using PortalRRHHFZ.Application.DTOs.Documentos;

namespace PortalRRHHFZ.Application.Interfaces.Documentos;

public interface IDocumentoColaboradorService
{
    Task<ApiResponse<IReadOnlyCollection<DocumentoColaboradorListDto>>> GetByColaboradorAsync(
        int colaboradorId,
        CancellationToken cancellationToken = default);

    Task<ApiResponse<DocumentoColaboradorDetailDto>> UploadAsync(
        int colaboradorId,
        UploadDocumentoRequest request,
        Stream archivo,
        string nombreArchivo,
        long archivoLength,
        string? contentType,
        ClaimsPrincipal principal,
        CancellationToken cancellationToken = default);

    Task<ApiResponse<DocumentoColaboradorDetailDto>> GetByIdAsync(
        int id,
        CancellationToken cancellationToken = default);

    Task<ApiResponse<DocumentoDownloadDto>> DownloadAsync(
        int id,
        CancellationToken cancellationToken = default);

    Task<ApiResponse<DocumentoColaboradorDetailDto>> UpdateAsync(
        int id,
        UpdateDocumentoRequest request,
        ClaimsPrincipal principal,
        CancellationToken cancellationToken = default);

    Task<ApiResponse<DocumentoColaboradorDetailDto>> DeactivateAsync(
        int id,
        ClaimsPrincipal principal,
        CancellationToken cancellationToken = default);
}
