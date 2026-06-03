namespace PortalRRHHFZ.Application.Interfaces.Storage;

public interface IFileStorageService
{
    Task<FileStorageResult> SaveAsync(
        Stream content,
        string relativeDirectory,
        string originalFileName,
        CancellationToken cancellationToken = default);

    Task<Stream?> OpenReadAsync(string relativePath, CancellationToken cancellationToken = default);

    bool Exists(string relativePath);
}

public sealed record FileStorageResult(string RelativePath);
