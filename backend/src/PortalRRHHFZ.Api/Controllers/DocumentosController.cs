using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PortalRRHHFZ.Application.Common;
using PortalRRHHFZ.Application.DTOs.Documentos;
using PortalRRHHFZ.Application.Interfaces.Documentos;

namespace PortalRRHHFZ.Api.Controllers;

[ApiController]
[Authorize(Policy = "RequireAdminOrRRHH")]
public sealed class DocumentosController(IDocumentoColaboradorService documentoService) : ControllerBase
{
    [HttpGet("api/colaboradores/{colaboradorId:int}/documentos")]
    public async Task<IActionResult> GetByColaborador(
        int colaboradorId,
        CancellationToken cancellationToken)
    {
        var response = await documentoService.GetByColaboradorAsync(colaboradorId, cancellationToken);
        return response.Success ? Ok(response) : NotFound(response);
    }

    [HttpPost("api/colaboradores/{colaboradorId:int}/documentos")]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> Upload(
        int colaboradorId,
        [FromForm] UploadDocumentoRequest request,
        IFormFile? archivo,
        CancellationToken cancellationToken)
    {
        if (archivo is null)
        {
            return BadRequest(ApiResponse<DocumentoColaboradorDetailDto>.Fail(
                "No fue posible subir el documento.",
                ["Archivo es obligatorio."]));
        }

        await using var stream = archivo.OpenReadStream();
        var response = await documentoService.UploadAsync(
            colaboradorId,
            request,
            stream,
            archivo.FileName,
            archivo.Length,
            archivo.ContentType,
            User,
            cancellationToken);

        return response.Success
            ? Created($"/api/documentos/{response.Data?.DocumentoColaboradorId}", response)
            : BadRequest(response);
    }

    [HttpGet("api/documentos/{id:int}")]
    public async Task<IActionResult> GetById(int id, CancellationToken cancellationToken)
    {
        var response = await documentoService.GetByIdAsync(id, cancellationToken);
        return response.Success ? Ok(response) : NotFound(response);
    }

    [HttpGet("api/documentos/{id:int}/descargar")]
    public async Task<IActionResult> Download(int id, CancellationToken cancellationToken)
    {
        var response = await documentoService.DownloadAsync(id, cancellationToken);

        if (!response.Success || response.Data is null)
        {
            return response.Message.Contains("no encontrado", StringComparison.OrdinalIgnoreCase)
                ? NotFound(response)
                : BadRequest(response);
        }

        return File(
            response.Data.Content,
            response.Data.ContentType,
            response.Data.FileName);
    }

    [HttpPut("api/documentos/{id:int}")]
    public async Task<IActionResult> Update(
        int id,
        UpdateDocumentoRequest request,
        CancellationToken cancellationToken)
    {
        var response = await documentoService.UpdateAsync(id, request, User, cancellationToken);
        return response.Success ? Ok(response) : BadRequest(response);
    }

    [HttpPatch("api/documentos/{id:int}/desactivar")]
    public async Task<IActionResult> Deactivate(int id, CancellationToken cancellationToken)
    {
        var response = await documentoService.DeactivateAsync(id, User, cancellationToken);
        return response.Success ? Ok(response) : BadRequest(response);
    }
}
