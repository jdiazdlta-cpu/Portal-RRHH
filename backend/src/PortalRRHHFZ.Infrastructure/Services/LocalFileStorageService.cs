using Microsoft.Extensions.Configuration;
using PortalRRHHFZ.Application.Interfaces.Storage;

namespace PortalRRHHFZ.Infrastructure.Services;

public sealed class LocalFileStorageService(
    IConfiguration configuration) : IFileStorageService
{
    private readonly string rootPath = ResolveRootPath(configuration);

    public async Task<FileStorageResult> SaveAsync(
        Stream content,
        string relativeDirectory,
        string originalFileName,
        CancellationToken cancellationToken = default)
    {
        var sanitizedDirectory = SanitizeRelativePath(relativeDirectory);
        var targetDirectory = Path.Combine(rootPath, sanitizedDirectory);
        Directory.CreateDirectory(targetDirectory);

        var extension = Path.GetExtension(originalFileName).ToLowerInvariant();
        var physicalFileName = $"{Guid.NewGuid():N}{extension}";
        var fullPath = Path.Combine(targetDirectory, physicalFileName);

        await using var fileStream = new FileStream(
            fullPath,
            FileMode.CreateNew,
            FileAccess.Write,
            FileShare.None,
            bufferSize: 81920,
            useAsync: true);

        await content.CopyToAsync(fileStream, cancellationToken);

        var relativePath = Path.Combine(sanitizedDirectory, physicalFileName)
            .Replace(Path.DirectorySeparatorChar, '/');

        return new FileStorageResult(relativePath);
    }

    public Task<Stream?> OpenReadAsync(
        string relativePath,
        CancellationToken cancellationToken = default)
    {
        var fullPath = GetFullPath(relativePath);

        if (!File.Exists(fullPath))
        {
            return Task.FromResult<Stream?>(null);
        }

        Stream stream = new FileStream(
            fullPath,
            FileMode.Open,
            FileAccess.Read,
            FileShare.Read,
            bufferSize: 81920,
            useAsync: true);

        return Task.FromResult<Stream?>(stream);
    }

    public bool Exists(string relativePath)
    {
        return File.Exists(GetFullPath(relativePath));
    }

    private string GetFullPath(string relativePath)
    {
        var sanitizedPath = SanitizeRelativePath(relativePath);
        var fullPath = Path.GetFullPath(Path.Combine(rootPath, sanitizedPath));
        var rootFullPath = Path.GetFullPath(rootPath);

        if (!fullPath.StartsWith(rootFullPath, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("La ruta solicitada esta fuera del almacenamiento permitido.");
        }

        return fullPath;
    }

    private static string ResolveRootPath(
        IConfiguration configuration)
    {
        var configuredPath = configuration["FileStorage:RootPath"] ?? "uploads";

        if (!string.IsNullOrWhiteSpace(configuredPath))
        {
            if (Path.IsPathRooted(configuredPath))
            {
                return Path.GetFullPath(configuredPath);
            }

            var existingPath = FindExistingAncestorPath(configuredPath);

            if (existingPath is not null)
            {
                return existingPath;
            }

            return Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), configuredPath));
        }

        return Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), "uploads"));
    }

    private static string SanitizeRelativePath(string relativePath)
    {
        return relativePath
            .Replace('\\', Path.DirectorySeparatorChar)
            .Replace('/', Path.DirectorySeparatorChar)
            .TrimStart(Path.DirectorySeparatorChar);
    }

    private static string? FindExistingAncestorPath(string relativePath)
    {
        foreach (var startPath in new[] { Directory.GetCurrentDirectory(), AppContext.BaseDirectory })
        {
            var directory = new DirectoryInfo(startPath);

            while (directory is not null)
            {
                var candidate = Path.GetFullPath(Path.Combine(directory.FullName, relativePath));

                if (Directory.Exists(candidate))
                {
                    return candidate;
                }

                directory = directory.Parent;
            }
        }

        return null;
    }
}
