using Microsoft.Extensions.Options;
using Nido.Application.Common.ProfileImages;
using Nido.Infrastructure.Storage;

namespace Nido.Infrastructure.ProfileImages;

public sealed class ConfigurableProfileImagePublicUrlResolver : IProfileImagePublicUrlResolver
{
    private readonly IOptions<ProfileImageOptions> _options;
    private readonly IOptions<SpacesOptions>? _spacesOptions;

    public ConfigurableProfileImagePublicUrlResolver(
        IOptions<ProfileImageOptions> options,
        IOptions<SpacesOptions>? spacesOptions = null)
    {
        _options = options;
        _spacesOptions = spacesOptions;
    }

    public string? Resolve(string? storageKey, DateTimeOffset? version = null)
    {
        if (string.IsNullOrWhiteSpace(storageKey))
        {
            return null;
        }

        var trimmedStorageKey = storageKey.Trim();
        var externalUrl = ProfileImageReferenceRules.NormalizeExternalUrlOrNull(trimmedStorageKey);
        if (externalUrl is not null)
        {
            return externalUrl;
        }

        if (Uri.TryCreate(trimmedStorageKey, UriKind.Absolute, out _))
        {
            return null;
        }

        if (trimmedStorageKey[0] == '/')
        {
            return trimmedStorageKey;
        }

        var baseUrl = _spacesOptions?.Value.Enabled == true && !string.IsNullOrWhiteSpace(_spacesOptions.Value.PublicBaseUrl)
            ? _spacesOptions.Value.PublicBaseUrl.TrimEnd('/')
            : _options.Value.PublicBaseUrl?.TrimEnd('/');

        if (string.IsNullOrWhiteSpace(baseUrl))
        {
            return null;
        }

        var url = $"{baseUrl}/{trimmedStorageKey}";

        if (version.HasValue)
        {
            url = $"{url}?v={version.Value.ToUnixTimeSeconds()}";
        }

        return url;
    }
}
