using Microsoft.Extensions.Options;
using Nido.Application.Common.ProfileImages;

namespace Nido.Infrastructure.ProfileImages;

public sealed class ConfigurableProfileImagePublicUrlResolver : IProfileImagePublicUrlResolver
{
    private readonly IOptions<ProfileImageOptions> _options;

    public ConfigurableProfileImagePublicUrlResolver(IOptions<ProfileImageOptions> options)
    {
        _options = options;
    }

    public string? Resolve(string? storageKey)
    {
        if (string.IsNullOrWhiteSpace(storageKey))
        {
            return null;
        }

        var baseUrl = _options.Value.PublicBaseUrl?.TrimEnd('/');
        if (string.IsNullOrWhiteSpace(baseUrl))
        {
            return null;
        }

        return $"{baseUrl}/{storageKey}";
    }
}
