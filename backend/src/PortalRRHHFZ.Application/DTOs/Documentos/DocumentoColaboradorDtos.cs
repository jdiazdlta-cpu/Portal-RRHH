namespace PortalRRHHFZ.Application.DTOs.Documentos;

public sealed class DocumentoColaboradorListDto
{
    public int DocumentoColaboradorId { get; init; }
    public int TipoDocumentoId { get; init; }
    public string TipoDocumentoNombre { get; init; } = string.Empty;
    public string NombreArchivo { get; init; } = string.Empty;
    public DateTime FechaCarga { get; init; }
    public DateTime? FechaVencimiento { get; init; }
    public bool TieneVencimiento { get; init; }
    public string? Observacion { get; init; }
    public bool IsActive { get; init; }
    public int SubidoPor { get; init; }
    public string SubidoPorNombre { get; init; } = string.Empty;
}

public sealed class DocumentoColaboradorDetailDto
{
    public int DocumentoColaboradorId { get; init; }
    public int ColaboradorId { get; init; }
    public string ColaboradorNombre { get; init; } = string.Empty;
    public int TipoDocumentoId { get; init; }
    public string TipoDocumentoNombre { get; init; } = string.Empty;
    public string NombreArchivo { get; init; } = string.Empty;
    public string RutaArchivo { get; init; } = string.Empty;
    public DateTime FechaCarga { get; init; }
    public DateTime? FechaVencimiento { get; init; }
    public bool TieneVencimiento { get; init; }
    public string? Observacion { get; init; }
    public int SubidoPor { get; init; }
    public string SubidoPorNombre { get; init; } = string.Empty;
    public DateTime CreatedAt { get; init; }
    public DateTime? UpdatedAt { get; init; }
    public string? CreatedBy { get; init; }
    public string? UpdatedBy { get; init; }
    public bool IsActive { get; init; }
}

public sealed class UploadDocumentoRequest
{
    public int TipoDocumentoId { get; init; }
    public bool TieneVencimiento { get; init; }
    public DateTime? FechaVencimiento { get; init; }
    public string? Observacion { get; init; }
}

public sealed class UpdateDocumentoRequest
{
    public int TipoDocumentoId { get; init; }
    public bool TieneVencimiento { get; init; }
    public DateTime? FechaVencimiento { get; init; }
    public string? Observacion { get; init; }
    public bool IsActive { get; init; }
}

public sealed class DocumentoDownloadDto
{
    public Stream Content { get; init; } = Stream.Null;
    public string FileName { get; init; } = string.Empty;
    public string ContentType { get; init; } = "application/octet-stream";
}
