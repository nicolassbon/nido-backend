namespace Nido.Application.Common.Storage;

public interface IFileStorageService
{
    Task<FileStorageUploadResult> UploadAsync(Stream stream, string key, string contentType, CancellationToken cancellationToken);
    Task DeleteAsync(string key, CancellationToken cancellationToken);
}

public sealed record FileStorageUploadResult(string Key, string PublicUrl);
