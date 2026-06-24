namespace Nido.Application.Common.Storage;

public static class StorageKeyRules
{
    private static readonly string[] AllowedPrefixes = ["products/", "electrodomesticos/", "recipes/", "avatars/", "catalog/", "usuarios/", "facturas/"];

    public static bool IsStorageKey(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        return !Uri.TryCreate(value, UriKind.Absolute, out _)
            && value[0] != '/'
            && !value.Contains("..", StringComparison.Ordinal)
            && AllowedPrefixes.Any(prefix => value.StartsWith(prefix, StringComparison.Ordinal));
    }
}
