using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PortalRRHHFZ.Application.Common;
using PortalRRHHFZ.Application.DTOs;
using PortalRRHHFZ.Application.Interfaces;
using PortalRRHHFZ.Domain.Constants;
using PortalRRHHFZ.Domain.Entities;
using PortalRRHHFZ.Infrastructure.Data;

namespace PortalRRHHFZ.Api.Controllers;

[ApiController]
[Authorize(Policy = AppPolicies.RequireAdminOrRRHH)]
public sealed class DocumentosController(AppDbContext db, IFileStorageService fileStorage) : ControllerBase
{
    private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".pdf", ".jpg", ".jpeg", ".png", ".doc", ".docx", ".xls", ".xlsx"
    };

    private const long MaxFileSize = 10 * 1024 * 1024;

    [HttpGet("api/colaboradores/{id:int}/documentos")]
    public async Task<IActionResult> GetByColaborador(int id, CancellationToken cancellationToken)
    {
        if (!await db.Colaboradores.AnyAsync(x => x.ColaboradorId == id, cancellationToken))
        {
            return NotFound(ApiResponse<object>.Fail("Colaborador no encontrado."));
        }

        var documentos = await db.DocumentosColaborador
            .Include(x => x.TipoDocumento)
            .Where(x => x.ColaboradorId == id && x.IsActive)
            .OrderByDescending(x => x.FechaCarga)
            .ToListAsync(cancellationToken);

        return Ok(ApiResponse<List<DocumentoDto>>.Ok(documentos.Select(x => x.ToDto()).ToList()));
    }

    [HttpPost("api/colaboradores/{id:int}/documentos")]
    [RequestSizeLimit(MaxFileSize)]
    public async Task<IActionResult> Upload(int id, [FromForm] UploadDocumentoForm form, CancellationToken cancellationToken)
    {
        if (form.Archivo is null || form.Archivo.Length == 0)
        {
            return BadRequest(ApiResponse<object>.Fail("Archivo es obligatorio."));
        }

        if (form.Archivo.Length > MaxFileSize)
        {
            return BadRequest(ApiResponse<object>.Fail("Archivo supera el maximo de 10 MB."));
        }

        var extension = Path.GetExtension(form.Archivo.FileName);
        if (!AllowedExtensions.Contains(extension))
        {
            return BadRequest(ApiResponse<object>.Fail("Tipo de archivo no permitido."));
        }

        if (!await db.Colaboradores.AnyAsync(x => x.ColaboradorId == id, cancellationToken))
        {
            return NotFound(ApiResponse<object>.Fail("Colaborador no encontrado."));
        }

        if (!await db.TiposDocumento.AnyAsync(x => x.TipoDocumentoId == form.TipoDocumentoId && x.IsActive, cancellationToken))
        {
            return BadRequest(ApiResponse<object>.Fail("Tipo de documento no valido."));
        }

        await using var stream = form.Archivo.OpenReadStream();
        var stored = await fileStorage.SaveAsync(id, stream, form.Archivo.FileName, cancellationToken);
        var documento = new DocumentoColaborador
        {
            ColaboradorId = id,
            TipoDocumentoId = form.TipoDocumentoId,
            NombreArchivo = stored.NombreArchivo,
            RutaArchivo = stored.RutaRelativa,
            FechaCarga = DateTime.UtcNow,
            TieneVencimiento = form.TieneVencimiento,
            FechaVencimiento = form.FechaVencimiento,
            Observacion = form.Observacion,
            SubidoPor = User.CurrentUserId(),
            CreatedBy = User.Identity?.Name
        };

        db.DocumentosColaborador.Add(documento);
        db.HistorialColaborador.Add(new HistorialColaborador
        {
            ColaboradorId = id,
            UsuarioId = User.CurrentUserId(),
            Accion = "DOCUMENTO_CARGADO",
            Campo = "Documento",
            ValorNuevo = stored.NombreArchivo,
            CreatedBy = User.Identity?.Name
        });
        await db.SaveChangesAsync(cancellationToken);
        await db.Entry(documento).Reference(x => x.TipoDocumento).LoadAsync(cancellationToken);

        return CreatedAtAction(nameof(GetDocumento), new { id = documento.DocumentoColaboradorId }, ApiResponse<DocumentoDto>.Ok(documento.ToDto(), "Documento cargado."));
    }

    [HttpGet("api/documentos/{id:int}")]
    public async Task<IActionResult> GetDocumento(int id, CancellationToken cancellationToken)
    {
        var documento = await db.DocumentosColaborador.Include(x => x.TipoDocumento).FirstOrDefaultAsync(x => x.DocumentoColaboradorId == id, cancellationToken);
        return documento is null
            ? NotFound(ApiResponse<object>.Fail("Documento no encontrado."))
            : Ok(ApiResponse<DocumentoDto>.Ok(documento.ToDto()));
    }

    [HttpGet("api/documentos/{id:int}/descargar")]
    public async Task<IActionResult> Descargar(int id, CancellationToken cancellationToken)
    {
        var documento = await db.DocumentosColaborador.FirstOrDefaultAsync(x => x.DocumentoColaboradorId == id && x.IsActive, cancellationToken);
        if (documento is null)
        {
            return NotFound(ApiResponse<object>.Fail("Documento no encontrado."));
        }

        var file = await fileStorage.ReadAsync(documento.RutaArchivo, cancellationToken);
        if (file is null)
        {
            return NotFound(ApiResponse<object>.Fail("Archivo fisico no encontrado."));
        }

        return File(file.Stream, file.ContentType, documento.NombreArchivo);
    }

    [HttpPut("api/documentos/{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateDocumentoRequest request, CancellationToken cancellationToken)
    {
        var documento = await db.DocumentosColaborador.Include(x => x.TipoDocumento).FirstOrDefaultAsync(x => x.DocumentoColaboradorId == id, cancellationToken);
        if (documento is null)
        {
            return NotFound(ApiResponse<object>.Fail("Documento no encontrado."));
        }

        if (!await db.TiposDocumento.AnyAsync(x => x.TipoDocumentoId == request.TipoDocumentoId && x.IsActive, cancellationToken))
        {
            return BadRequest(ApiResponse<object>.Fail("Tipo de documento no valido."));
        }

        documento.TipoDocumentoId = request.TipoDocumentoId;
        documento.TieneVencimiento = request.TieneVencimiento;
        documento.FechaVencimiento = request.FechaVencimiento;
        documento.Observacion = request.Observacion;
        documento.UpdatedBy = User.Identity?.Name;
        await db.SaveChangesAsync(cancellationToken);
        await db.Entry(documento).Reference(x => x.TipoDocumento).LoadAsync(cancellationToken);

        return Ok(ApiResponse<DocumentoDto>.Ok(documento.ToDto(), "Documento actualizado."));
    }

    [HttpPatch("api/documentos/{id:int}/desactivar")]
    public async Task<IActionResult> Desactivar(int id, CancellationToken cancellationToken)
    {
        var documento = await db.DocumentosColaborador.Include(x => x.TipoDocumento).FirstOrDefaultAsync(x => x.DocumentoColaboradorId == id, cancellationToken);
        if (documento is null)
        {
            return NotFound(ApiResponse<object>.Fail("Documento no encontrado."));
        }

        documento.IsActive = false;
        documento.UpdatedBy = User.Identity?.Name;
        await db.SaveChangesAsync(cancellationToken);
        return Ok(ApiResponse<DocumentoDto>.Ok(documento.ToDto(), "Documento desactivado."));
    }
}

public sealed class UploadDocumentoForm
{
    public int TipoDocumentoId { get; set; }
    public bool TieneVencimiento { get; set; }
    public DateTime? FechaVencimiento { get; set; }
    public string? Observacion { get; set; }
    public IFormFile? Archivo { get; set; }
}
