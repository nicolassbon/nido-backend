using Nido.Application.Common.Images;

namespace Nido.Api.ImageUploads;

internal static class ImageUploadFormReader
{
    private static readonly HashSet<string> AllowedContentTypes = ["image/jpeg", "image/png", "image/webp"];

    public static async Task<ImageUpload> ReadAsync(IFormFile? file, long maxBytes, CancellationToken cancellationToken)
    {
        if (file is null || file.Length == 0)
        {
            throw new MissingImageFileException();
        }

        if (file.Length > maxBytes)
        {
            throw new ImageSizeExceededException();
        }

        if (string.IsNullOrWhiteSpace(file.ContentType) || !AllowedContentTypes.Contains(file.ContentType.ToLowerInvariant()))
        {
            throw new UnsupportedImageTypeException();
        }

        await using var memoryStream = new MemoryStream();
        await file.CopyToAsync(memoryStream, cancellationToken);
        return new ImageUpload(file.FileName, file.ContentType, memoryStream.ToArray());
    }
}
