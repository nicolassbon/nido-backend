namespace Nido.Application.Common.ProfileImages;

public interface IProfileImageStorage
{
    Task UploadAsync(string storageKey, byte[] content, string contentType, CancellationToken cancellationToken);
    Task DeleteAsync(string storageKey, CancellationToken cancellationToken);
}
