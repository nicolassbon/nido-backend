using Nido.Application.Common.Storage;

namespace Nido.Application.Common.ProfileImages;

public static class ProfileImageReferenceRules
{
    public static string? NormalizeExternalUrlOrNull(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var trimmedValue = value.Trim();

        if (!Uri.TryCreate(trimmedValue, UriKind.Absolute, out var uri))
        {
            return null;
        }

        return uri.Scheme == Uri.UriSchemeHttps
            ? trimmedValue
            : null;
    }

    public static bool IsManagedStorageKey(string? value)
        => StorageKeyRules.IsStorageKey(value);
}
