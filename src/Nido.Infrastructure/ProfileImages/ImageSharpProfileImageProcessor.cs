using Microsoft.Extensions.Options;
using Nido.Application.Auth;
using Nido.Application.Common.ProfileImages;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.Formats.Webp;
using SixLabors.ImageSharp.Processing;

namespace Nido.Infrastructure.ProfileImages;

public sealed class ImageSharpProfileImageProcessor : IProfileImageProcessor
{
    private static readonly HashSet<string> AllowedContentTypes =
    [
        "image/jpeg",
        "image/png",
        "image/webp"
    ];

    private readonly ProfileImageOptions _options;

    public ImageSharpProfileImageProcessor(IOptions<ProfileImageOptions> options)
    {
        _options = options.Value;
    }

    public async Task<ProcessedProfileImage> ProcessAsync(RegistrationProfileImageUpload upload, CancellationToken cancellationToken)
    {
        if (upload.Content.Length == 0)
        {
            throw new ArgumentException("Invalid image content.");
        }

        if (upload.Content.Length > _options.MaxBytes)
        {
            throw new ArgumentException("Profile image exceeds the allowed limit.");
        }

        if (string.IsNullOrWhiteSpace(upload.ContentType) || !AllowedContentTypes.Contains(upload.ContentType.ToLowerInvariant()))
        {
            throw new ArgumentException("Unsupported image type.");
        }

        IImageFormat format;
        try
        {
            using var metadataStream = new MemoryStream(upload.Content);
            var info = await Image.IdentifyAsync(metadataStream, cancellationToken);
            if (info is null)
            {
                throw new ArgumentException("Invalid image content.");
            }
            if (info.Width > 4096 || info.Height > 4096)
            {
                throw new ArgumentException("Image dimensions exceed the allowed limit of 4096 pixels.");
            }
            format = info.Metadata.DecodedImageFormat ?? throw new ArgumentException("Invalid image content.");
        }
        catch (ArgumentException)
        {
            throw;
        }
        catch (Exception ex)
        {
            throw new ArgumentException("Invalid image content.", ex);
        }

        if (format is not JpegFormat && format is not PngFormat && format is not WebpFormat)
        {
            throw new ArgumentException("Unsupported image type.");
        }

        try
        {
            using var inputStream = new MemoryStream(upload.Content);
            using var image = await Image.LoadAsync(inputStream, cancellationToken);

            image.Mutate(x =>
            {
                x.AutoOrient();
                x.Resize(new ResizeOptions
                {
                    Mode = ResizeMode.Max,
                    Size = new Size(_options.MaxDimension, _options.MaxDimension)
                });
            });

            await using var output = new MemoryStream();
            await image.SaveAsWebpAsync(output, new WebpEncoder { Quality = _options.WebpQuality }, cancellationToken);

            var bytes = output.ToArray();
            return new ProcessedProfileImage(bytes, "image/webp", image.Width, image.Height, bytes.LongLength);
        }
        catch (Exception ex)
        {
            throw new ArgumentException("Invalid image content.", ex);
        }
    }
}
