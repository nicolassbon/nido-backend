using Nido.Application.Common.ProfileImages;

namespace Nido.Infrastructure.ProfileImages;

public sealed class LocalProfileImageStorage : IProfileImageStorage
{
    private static readonly string BasePath = Path.Combine(AppContext.BaseDirectory, "wwwroot", "uploads");
    private static readonly string CanonicalBasePath = GetCanonicalBasePath();

    private static string GetCanonicalBasePath()
    {
        var path = Path.GetFullPath(BasePath);
        return path.EndsWith(Path.DirectorySeparatorChar) ? path : path + Path.DirectorySeparatorChar;
    }

    private string GetValidatedPath(string storageKey)
    {
        var combinedPath = Path.Combine(BasePath, storageKey.Replace('/', Path.DirectorySeparatorChar));
        var fullPath = Path.GetFullPath(combinedPath);

        if (!fullPath.StartsWith(CanonicalBasePath, StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException("Path traversal attempt detected.", nameof(storageKey));
        }

        return fullPath;
    }

    public Task UploadAsync(string storageKey, byte[] content, string contentType, CancellationToken cancellationToken)
    {
        var filePath = GetValidatedPath(storageKey);
        var directory = Path.GetDirectoryName(filePath);
        if (directory is not null)
        {
            Directory.CreateDirectory(directory);
        }
        return File.WriteAllBytesAsync(filePath, content, cancellationToken);
    }

    public Task DeleteAsync(string storageKey, CancellationToken cancellationToken)
    {
        var filePath = GetValidatedPath(storageKey);
        if (File.Exists(filePath))
        {
            File.Delete(filePath);
        }
        return Task.CompletedTask;
    }
}
