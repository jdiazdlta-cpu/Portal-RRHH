using PortalRRHHFZ.Application.Interfaces;

namespace PortalRRHHFZ.Infrastructure.Storage;

public sealed class LocalFileStorageService : IFileStorageService
{
    private readonly string _uploadsRoot;

    public LocalFileStorageService(IConfigurationAccessor configuration)
    {
        var configuredRoot = configuration.GetValue("FileStorage:UploadsRoot") ?? "../../../../uploads";
        _uploadsRoot = Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), configuredRoot));
        Directory.CreateDirectory(_uploadsRoot);
    }

    public async Task<StoredFileResult> SaveAsync(int colaboradorId, Stream fileStream, string originalFileName, CancellationToken cancellationToken = default)
    {
        var extension = Path.GetExtension(originalFileName).ToLowerInvariant();
        var safeFileName = $"{Guid.NewGuid():N}{extension}";
        var relativeDirectory = Path.Combine("colaboradores", colaboradorId.ToString());
        var physicalDirectory = Path.Combine(_uploadsRoot, relativeDirectory);
        Directory.CreateDirectory(physicalDirectory);

        var physicalPath = Path.Combine(physicalDirectory, safeFileName);
        await using var output = File.Create(physicalPath);
        await fileStream.CopyToAsync(output, cancellationToken);

        return new StoredFileResult(originalFileName, Path.Combine(relativeDirectory, safeFileName).Replace('\\', '/'));
    }

    public Task<StoredFileReadResult?> ReadAsync(string rutaRelativa, CancellationToken cancellationToken = default)
    {
        var normalized = rutaRelativa.Replace('/', Path.DirectorySeparatorChar);
        var fullPath = Path.GetFullPath(Path.Combine(_uploadsRoot, normalized));
        if (!fullPath.StartsWith(_uploadsRoot, StringComparison.OrdinalIgnoreCase) || !File.Exists(fullPath))
        {
            return Task.FromResult<StoredFileReadResult?>(null);
        }

        var contentType = Path.GetExtension(fullPath).ToLowerInvariant() switch
        {
            ".pdf" => "application/pdf",
            ".jpg" or ".jpeg" => "image/jpeg",
            ".png" => "image/png",
            ".doc" => "application/msword",
            ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
            ".xls" => "application/vnd.ms-excel",
            ".xlsx" => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            _ => "application/octet-stream"
        };

        return Task.FromResult<StoredFileReadResult?>(new StoredFileReadResult(File.OpenRead(fullPath), contentType));
    }
}

public sealed class IConfigurationAccessor(Microsoft.Extensions.Configuration.IConfiguration configuration)
{
    public string? GetValue(string key) => configuration[key];
}
