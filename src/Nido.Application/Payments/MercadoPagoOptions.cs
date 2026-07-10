namespace Nido.Application.Payments;

public sealed class MercadoPagoOptions
{
    public const string SectionName = "MercadoPago";

    private const string SuccessPath = "/perfil?status=success";
    private const string PendingPath = "/perfil?status=pending";
    private const string FailurePath = "/perfil?status=failure";

    public string AccessToken { get; init; } = string.Empty;

    public string WebhookSecret { get; init; } = string.Empty;

    public MercadoPagoMode? Mode { get; init; }

    public string PublicKey { get; init; } = string.Empty;

    public string ApiBaseUrl { get; init; } = "https://api.mercadopago.com";

    public decimal UnitPrice { get; init; } = 1.00m;

    public string SuccessUrl { get; set; } = string.Empty;

    public string PendingUrl { get; set; } = string.Empty;

    public string FailureUrl { get; set; } = string.Empty;

    public void ApplyFrontendRedirectDefaults(string? frontendBaseUrl)
    {
        if (string.IsNullOrWhiteSpace(frontendBaseUrl))
        {
            return;
        }

        SuccessUrl = UseConfiguredOrDerived(SuccessUrl, frontendBaseUrl, SuccessPath);
        PendingUrl = UseConfiguredOrDerived(PendingUrl, frontendBaseUrl, PendingPath);
        FailureUrl = UseConfiguredOrDerived(FailureUrl, frontendBaseUrl, FailurePath);
    }

    public static bool HasApprovedProductionApiBaseUrl(MercadoPagoOptions options)
        => Uri.TryCreate(options.ApiBaseUrl, UriKind.Absolute, out var uri)
           && uri.Scheme.Equals(Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase)
           && uri.Host.Equals("api.mercadopago.com", StringComparison.OrdinalIgnoreCase)
           && uri.UserInfo.Length == 0;

    public static bool HasValidMode(MercadoPagoOptions options)
        => options.Mode is not null && Enum.IsDefined(options.Mode.Value);

    private static string UseConfiguredOrDerived(string configuredUrl, string frontendBaseUrl, string path)
        => string.IsNullOrWhiteSpace(configuredUrl)
            ? BuildFrontendUrl(frontendBaseUrl, path) ?? string.Empty
            : configuredUrl;

    private static string? BuildFrontendUrl(string frontendBaseUrl, string path)
    {
        if (!Uri.TryCreate(frontendBaseUrl, UriKind.Absolute, out var uri))
        {
            return null;
        }

        return string.Concat(uri.ToString().TrimEnd('/'), path);
    }
}

public enum MercadoPagoMode
{
    Sandbox,
    Production
}
