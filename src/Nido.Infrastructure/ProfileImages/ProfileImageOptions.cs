namespace Nido.Infrastructure.ProfileImages;

public sealed class ProfileImageOptions
{
    public const string SectionName = "ProfileImages";

    public long MaxBytes { get; init; } = 5 * 1024 * 1024;
    public int MaxDimension { get; init; } = 512;
    public int WebpQuality { get; init; } = 80;
    public string PublicBaseUrl { get; init; } = string.Empty;
}


