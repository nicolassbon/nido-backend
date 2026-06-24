using Microsoft.Extensions.Options;
using Nido.Application.Common.Images;
using Nido.Infrastructure.Storage;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.Formats.Webp;
using SixLabors.ImageSharp.Processing;

namespace Nido.Infrastructure.Images;

public sealed class ImageSharpImageProcessingService : IImageProcessingService
{
    private static readonly HashSet<string> AllowedContentTypes = ["image/jpeg", "image/png", "image/webp"];
    private readonly SpacesOptions _options;

    public ImageSharpImageProcessingService(IOptions<SpacesOptions> options)
    {
        _options = options.Value;
    }

    public async Task<ProcessedImage> ProcessAsync(ImageUpload upload, CancellationToken cancellationToken)
    {
        if (upload.Content.Length == 0)
        {
            throw new MissingImageFileException();
        }

        if (upload.Content.Length > _options.MaxUploadBytes)
        {
            throw new ImageSizeExceededException();
        }

        if (!AllowedContentTypes.Contains(upload.ContentType.ToLowerInvariant()))
        {
            throw new UnsupportedImageTypeException();
        }

        try
        {
            using var input = new MemoryStream(upload.Content);
            using var image = await Image.LoadAsync(input, cancellationToken);
            if (image.Metadata.DecodedImageFormat is not JpegFormat and not PngFormat and not WebpFormat)
            {
                throw new UnsupportedImageTypeException();
            }

            image.Mutate(x => x.AutoOrient());

            await using var output = new MemoryStream();
            await image.SaveAsWebpAsync(output, new WebpEncoder { Quality = 82 }, cancellationToken);
            var bytes = output.ToArray();
            return new ProcessedImage(bytes, "image/webp", image.Width, image.Height, bytes.LongLength);
        }
        catch (ImageUploadException)
        {
            throw;
        }
        catch
        {
            throw new UnsupportedImageTypeException();
        }
    }
}
