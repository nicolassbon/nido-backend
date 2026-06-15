namespace Nido.Application.Common.Images;

public sealed record ImageUpload(string FileName, string ContentType, byte[] Content);

public sealed record ProcessedImage(byte[] Content, string ContentType, int Width, int Height, long Length)
{
    public Stream OpenReadStream() => new MemoryStream(Content, writable: false);
}

public interface IImageProcessingService
{
    Task<ProcessedImage> ProcessAsync(ImageUpload upload, CancellationToken cancellationToken);
}
