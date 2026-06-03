using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using PortalRRHHFZ.Application.Common;
using PortalRRHHFZ.Application.DTOs.Documentos;
using PortalRRHHFZ.Application.Interfaces.Documentos;
using PortalRRHHFZ.Application.Interfaces.Storage;
using PortalRRHHFZ.Domain.Entities;
using PortalRRHHFZ.Infrastructure.Data;

namespace PortalRRHHFZ.Infrastructure.Services;

public sealed class DocumentoColaboradorService(
    AppDbContext dbContext,
    IFileStorageService fileStorageService) : IDocumentoColaboradorService
{
    private const long MaxFileSizeBytes = 10 * 1024 * 1024;

    private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".pdf",
        ".jpg",
        ".jpeg",
        ".png",
        ".doc",
        ".docx",
        ".xls",
        ".xlsx"
    };

    public async Task<ApiResponse<IReadOnlyCollection<DocumentoColaboradorListDto>>> GetByColaboradorAsync(
        int colaboradorId,
        CancellationToken cancellationToken = default)
    {
        var colaboradorExists = await dbContext.Colaboradores.AnyAsync(
            colaborador => colaborador.ColaboradorId == colaboradorId,
            cancellationToken);

        if (!colaboradorExists)
        {
            return ApiResponse<IReadOnlyCollection<DocumentoColaboradorListDto>>.Fail("Colaborador no encontrado.");
        }

        var documentos = await BaseQuery()
            .AsNoTracking()
            .Where(documento => documento.ColaboradorId == colaboradorId)
            .OrderByDescending(documento => documento.FechaCarga)
            .Select(documento => new DocumentoColaboradorListDto
            {
                DocumentoColaboradorId = documento.DocumentoColaboradorId,
                TipoDocumentoId = documento.TipoDocumentoId,
                TipoDocumentoNombre = documento.TipoDocumento.Nombre,
                NombreArchivo = documento.NombreArchivo,
                FechaCarga = documento.FechaCarga,
                FechaVencimiento = documento.FechaVencimiento,
                TieneVencimiento = documento.TieneVencimiento,
                Observacion = documento.Observacion,
                IsActive = documento.IsActive,
                SubidoPor = documento.SubidoPor,
                SubidoPorNombre = documento.UsuarioSubida.NombreUsuario
            })
            .ToListAsync(cancellationToken);

        return ApiResponse<IReadOnlyCollection<DocumentoColaboradorListDto>>.Ok(documentos);
    }

    public async Task<ApiResponse<DocumentoColaboradorDetailDto>> UploadAsync(
        int colaboradorId,
        UploadDocumentoRequest request,
        Stream archivo,
        string nombreArchivo,
        long archivoLength,
        string? contentType,
        ClaimsPrincipal principal,
        CancellationToken cancellationToken = default)
    {
        var currentUser = GetCurrentUser(principal);

        if (currentUser.UserId is null)
        {
            return ApiResponse<DocumentoColaboradorDetailDto>.Fail("Usuario autenticado invalido.");
        }

        var errors = await ValidateUploadAsync(
            colaboradorId,
            request,
            nombreArchivo,
            archivoLength,
            cancellationToken);

        if (errors.Count > 0)
        {
            return ApiResponse<DocumentoColaboradorDetailDto>.Fail("No fue posible subir el documento.", errors);
        }

        var storageResult = await fileStorageService.SaveAsync(
            archivo,
            $"colaboradores/{colaboradorId}",
            nombreArchivo,
            cancellationToken);

        var now = DateTime.UtcNow;
        var documento = new DocumentoColaborador
        {
            ColaboradorId = colaboradorId,
            TipoDocumentoId = request.TipoDocumentoId,
            NombreArchivo = Path.GetFileName(nombreArchivo),
            RutaArchivo = storageResult.RelativePath,
            FechaCarga = now,
            FechaVencimiento = request.TieneVencimiento ? request.FechaVencimiento : null,
            TieneVencimiento = request.TieneVencimiento,
            Observacion = NormalizeNullable(request.Observacion),
            SubidoPor = currentUser.UserId.Value,
            CreatedAt = now,
            CreatedBy = currentUser.UserName,
            UpdatedAt = now,
            UpdatedBy = currentUser.UserName,
            IsActive = true
        };

        dbContext.DocumentosColaborador.Add(documento);
        await dbContext.SaveChangesAsync(cancellationToken);

        return await GetDetailResponseAsync(documento.DocumentoColaboradorId, "Documento subido correctamente.", cancellationToken);
    }

    public async Task<ApiResponse<DocumentoColaboradorDetailDto>> GetByIdAsync(
        int id,
        CancellationToken cancellationToken = default)
    {
        var documento = await BaseQuery()
            .AsNoTracking()
            .SingleOrDefaultAsync(item => item.DocumentoColaboradorId == id, cancellationToken);

        return documento is null
            ? ApiResponse<DocumentoColaboradorDetailDto>.Fail("Documento no encontrado.")
            : ApiResponse<DocumentoColaboradorDetailDto>.Ok(ToDetailDto(documento));
    }

    public async Task<ApiResponse<DocumentoDownloadDto>> DownloadAsync(
        int id,
        CancellationToken cancellationToken = default)
    {
        var documento = await dbContext.DocumentosColaborador
            .AsNoTracking()
            .SingleOrDefaultAsync(item => item.DocumentoColaboradorId == id, cancellationToken);

        if (documento is null)
        {
            return ApiResponse<DocumentoDownloadDto>.Fail("Documento no encontrado.");
        }

        if (!documento.IsActive)
        {
            return ApiResponse<DocumentoDownloadDto>.Fail("El documento esta inactivo.");
        }

        if (!fileStorageService.Exists(documento.RutaArchivo))
        {
            return ApiResponse<DocumentoDownloadDto>.Fail("El archivo fisico no existe.");
        }

        var stream = await fileStorageService.OpenReadAsync(documento.RutaArchivo, cancellationToken);

        if (stream is null)
        {
            return ApiResponse<DocumentoDownloadDto>.Fail("El archivo fisico no existe.");
        }

        return ApiResponse<DocumentoDownloadDto>.Ok(new DocumentoDownloadDto
        {
            Content = stream,
            FileName = documento.NombreArchivo,
            ContentType = GetContentType(documento.NombreArchivo)
        });
    }

    public async Task<ApiResponse<DocumentoColaboradorDetailDto>> UpdateAsync(
        int id,
        UpdateDocumentoRequest request,
        ClaimsPrincipal principal,
        CancellationToken cancellationToken = default)
    {
        var documento = await dbContext.DocumentosColaborador
            .SingleOrDefaultAsync(item => item.DocumentoColaboradorId == id, cancellationToken);

        if (documento is null)
        {
            return ApiResponse<DocumentoColaboradorDetailDto>.Fail("Documento no encontrado.");
        }

        var errors = await ValidateMetadataAsync(
            request.TipoDocumentoId,
            request.TieneVencimiento,
            request.FechaVencimiento,
            cancellationToken);

        if (errors.Count > 0)
        {
            return ApiResponse<DocumentoColaboradorDetailDto>.Fail("No fue posible actualizar el documento.", errors);
        }

        documento.TipoDocumentoId = request.TipoDocumentoId;
        documento.TieneVencimiento = request.TieneVencimiento;
        documento.FechaVencimiento = request.TieneVencimiento ? request.FechaVencimiento : null;
        documento.Observacion = NormalizeNullable(request.Observacion);
        documento.IsActive = request.IsActive;
        documento.UpdatedAt = DateTime.UtcNow;
        documento.UpdatedBy = principal.Identity?.Name;

        await dbContext.SaveChangesAsync(cancellationToken);

        return await GetDetailResponseAsync(documento.DocumentoColaboradorId, "Documento actualizado correctamente.", cancellationToken);
    }

    public async Task<ApiResponse<DocumentoColaboradorDetailDto>> DeactivateAsync(
        int id,
        ClaimsPrincipal principal,
        CancellationToken cancellationToken = default)
    {
        var documento = await dbContext.DocumentosColaborador
            .SingleOrDefaultAsync(item => item.DocumentoColaboradorId == id, cancellationToken);

        if (documento is null)
        {
            return ApiResponse<DocumentoColaboradorDetailDto>.Fail("Documento no encontrado.");
        }

        documento.IsActive = false;
        documento.UpdatedAt = DateTime.UtcNow;
        documento.UpdatedBy = principal.Identity?.Name;

        await dbContext.SaveChangesAsync(cancellationToken);

        return await GetDetailResponseAsync(documento.DocumentoColaboradorId, "Documento desactivado correctamente.", cancellationToken);
    }

    private IQueryable<DocumentoColaborador> BaseQuery()
    {
        return dbContext.DocumentosColaborador
            .Include(documento => documento.TipoDocumento)
            .Include(documento => documento.Colaborador)
            .Include(documento => documento.UsuarioSubida);
    }

    private async Task<ApiResponse<DocumentoColaboradorDetailDto>> GetDetailResponseAsync(
        int id,
        string message,
        CancellationToken cancellationToken)
    {
        var documento = await BaseQuery()
            .AsNoTracking()
            .SingleAsync(item => item.DocumentoColaboradorId == id, cancellationToken);

        return ApiResponse<DocumentoColaboradorDetailDto>.Ok(ToDetailDto(documento), message);
    }

    private async Task<List<string>> ValidateUploadAsync(
        int colaboradorId,
        UploadDocumentoRequest request,
        string nombreArchivo,
        long archivoLength,
        CancellationToken cancellationToken)
    {
        var errors = new List<string>();

        var colaboradorActivo = await dbContext.Colaboradores.AnyAsync(
            colaborador => colaborador.ColaboradorId == colaboradorId && colaborador.IsActive,
            cancellationToken);

        if (!colaboradorActivo)
        {
            errors.Add("ColaboradorId no existe o esta inactivo.");
        }

        errors.AddRange(await ValidateMetadataAsync(
            request.TipoDocumentoId,
            request.TieneVencimiento,
            request.FechaVencimiento,
            cancellationToken));
        errors.AddRange(ValidateFile(nombreArchivo, archivoLength));

        return errors;
    }

    private async Task<List<string>> ValidateMetadataAsync(
        int tipoDocumentoId,
        bool tieneVencimiento,
        DateTime? fechaVencimiento,
        CancellationToken cancellationToken)
    {
        var errors = new List<string>();

        if (tipoDocumentoId <= 0)
        {
            errors.Add("TipoDocumentoId es obligatorio.");
        }
        else
        {
            var tipoDocumentoActivo = await dbContext.TiposDocumento.AnyAsync(
                tipoDocumento => tipoDocumento.TipoDocumentoId == tipoDocumentoId && tipoDocumento.IsActive,
                cancellationToken);

            if (!tipoDocumentoActivo)
            {
                errors.Add("TipoDocumentoId no existe o esta inactivo.");
            }
        }

        if (tieneVencimiento && !fechaVencimiento.HasValue)
        {
            errors.Add("FechaVencimiento es obligatoria cuando TieneVencimiento es true.");
        }

        return errors;
    }

    private static List<string> ValidateFile(string nombreArchivo, long archivoLength)
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(nombreArchivo))
        {
            errors.Add("Archivo es obligatorio.");
            return errors;
        }

        if (archivoLength <= 0)
        {
            errors.Add("El archivo esta vacio.");
        }

        if (archivoLength > MaxFileSizeBytes)
        {
            errors.Add("El archivo supera el tamano maximo permitido de 10 MB.");
        }

        var extension = Path.GetExtension(nombreArchivo);

        if (string.IsNullOrWhiteSpace(extension) || !AllowedExtensions.Contains(extension))
        {
            errors.Add("Extension de archivo no permitida.");
        }

        return errors;
    }

    private static DocumentoColaboradorDetailDto ToDetailDto(DocumentoColaborador documento)
    {
        return new DocumentoColaboradorDetailDto
        {
            DocumentoColaboradorId = documento.DocumentoColaboradorId,
            ColaboradorId = documento.ColaboradorId,
            ColaboradorNombre = GetNombreCompleto(documento.Colaborador),
            TipoDocumentoId = documento.TipoDocumentoId,
            TipoDocumentoNombre = documento.TipoDocumento.Nombre,
            NombreArchivo = documento.NombreArchivo,
            RutaArchivo = documento.RutaArchivo,
            FechaCarga = documento.FechaCarga,
            FechaVencimiento = documento.FechaVencimiento,
            TieneVencimiento = documento.TieneVencimiento,
            Observacion = documento.Observacion,
            SubidoPor = documento.SubidoPor,
            SubidoPorNombre = documento.UsuarioSubida.NombreUsuario,
            CreatedAt = documento.CreatedAt,
            UpdatedAt = documento.UpdatedAt,
            CreatedBy = documento.CreatedBy,
            UpdatedBy = documento.UpdatedBy,
            IsActive = documento.IsActive
        };
    }

    private static CurrentUser GetCurrentUser(ClaimsPrincipal principal)
    {
        var userIdValue = principal.FindFirstValue("UserId")
            ?? principal.FindFirstValue(ClaimTypes.NameIdentifier);

        return new CurrentUser(
            int.TryParse(userIdValue, out var userId) ? userId : null,
            principal.Identity?.Name);
    }

    private static string GetNombreCompleto(Colaborador colaborador)
    {
        return string.Join(
            " ",
            new[]
            {
                colaborador.PrimerNombre,
                colaborador.SegundoNombre,
                colaborador.PrimerApellido,
                colaborador.SegundoApellido
            }.Where(value => !string.IsNullOrWhiteSpace(value)));
    }

    private static string? NormalizeNullable(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    private static string GetContentType(string fileName)
    {
        return Path.GetExtension(fileName).ToLowerInvariant() switch
        {
            ".pdf" => "application/pdf",
            ".jpg" => "image/jpeg",
            ".jpeg" => "image/jpeg",
            ".png" => "image/png",
            ".doc" => "application/msword",
            ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
            ".xls" => "application/vnd.ms-excel",
            ".xlsx" => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            _ => "application/octet-stream"
        };
    }

    private sealed record CurrentUser(int? UserId, string? UserName);
}
