using Microsoft.Extensions.Options;
using Nido.Application.Common.Assets;
using Nido.Infrastructure.Storage;

namespace Nido.Infrastructure.PublicAssets;

public sealed class SpacesPublicAssetUrlResolver : IPublicAssetUrlResolver
{
    private readonly SpacesOptions _options;

    public SpacesPublicAssetUrlResolver(IOptions<SpacesOptions> options)
    {
        _options = options.Value;
    }

    public string? Resolve(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        if (Uri.TryCreate(value, UriKind.Absolute, out _) || value[0] == '/')
        {
            return value;
        }

        var baseUrl = _options.PublicBaseUrl.TrimEnd('/');
        return string.IsNullOrWhiteSpace(baseUrl) ? null : $"{baseUrl}/{value}";
    }
}
