namespace PortalRRHHFZ.Application.Interfaces;

public sealed record StoredFileResult(string NombreArchivo, string RutaRelativa);
public sealed record StoredFileReadResult(Stream Stream, string ContentType);

public interface IFileStorageService
{
    Task<StoredFileResult> SaveAsync(int colaboradorId, Stream fileStream, string originalFileName, CancellationToken cancellationToken = default);
    Task<StoredFileReadResult?> ReadAsync(string rutaRelativa, CancellationToken cancellationToken = default);
}
