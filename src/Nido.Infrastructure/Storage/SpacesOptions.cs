namespace Nido.Infrastructure.Storage;

public sealed class SpacesOptions
{
    public const string SectionName = "Spaces";

    public string Bucket { get; init; } = string.Empty;
    public string Endpoint { get; init; } = "https://nyc3.digitaloceanspaces.com";
    public string Region { get; init; } = "nyc3";
    public string PublicBaseUrl { get; init; } = string.Empty;
    public string AccessKey { get; init; } = string.Empty;
    public string SecretKey { get; init; } = string.Empty;
    public long MaxUploadBytes { get; init; } = 5 * 1024 * 1024;
    public bool Enabled { get; init; }

    public bool HasUploadConfiguration()
        => !string.IsNullOrWhiteSpace(Bucket)
           && !string.IsNullOrWhiteSpace(Endpoint)
           && !string.IsNullOrWhiteSpace(PublicBaseUrl)
           && !string.IsNullOrWhiteSpace(AccessKey)
           && !string.IsNullOrWhiteSpace(SecretKey);
}
