namespace Nido.Application.Payments;

public sealed class FrontendOptions
{
    public const string SectionName = "Frontend";

    public string BaseUrl { get; init; } = string.Empty;

    public static bool HasApprovedProductionBaseUrl(FrontendOptions options)
        => Uri.TryCreate(options.BaseUrl, UriKind.Absolute, out var uri)
           && uri.Scheme.Equals(Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase)
           && !uri.IsLoopback
           && uri.UserInfo.Length == 0;
}
