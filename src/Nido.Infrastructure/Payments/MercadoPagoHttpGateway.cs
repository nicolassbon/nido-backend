using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.RegularExpressions;
using System.Text.Json;
using Microsoft.Extensions.Options;
using Nido.Application.Payments;

namespace Nido.Infrastructure.Payments;

public sealed class MercadoPagoHttpGateway : IMercadoPagoGateway
{
    private readonly HttpClient _httpClient;
    private readonly MercadoPagoOptions _options;

    public MercadoPagoHttpGateway(HttpClient httpClient, IOptions<MercadoPagoOptions> options)
    {
        _httpClient = httpClient;
        _options = options.Value;
    }

    public async Task<MercadoPagoCheckoutPreference> CreateCheckoutPreferenceAsync(MercadoPagoCheckoutPreferenceRequest request, CancellationToken ct)
    {
        EnsureConfigured();

        var payload = new Dictionary<string, object?>
        {
            ["external_reference"] = request.HogarId.ToString(),
            ["items"] = new[]
            {
                new
                {
                    title = "Nido Premium",
                    quantity = 1,
                    currency_id = "ARS",
                    unit_price = _options.UnitPrice
                }
            }
        };

        if (HasSuitableBackUrls())
        {
            payload["back_urls"] = new
            {
                success = _options.SuccessUrl,
                pending = _options.PendingUrl,
                failure = _options.FailureUrl
            };
            payload["auto_return"] = "approved";
        }

        using var message = new HttpRequestMessage(HttpMethod.Post, "/checkout/preferences")
        {
            Content = JsonContent.Create(payload)
        };

        using var response = await _httpClient.SendAsync(message, ct);
        if (!response.IsSuccessStatusCode)
        {
            throw await CreateProviderExceptionAsync("create preference", response, ct);
        }

        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync(ct));
        var root = document.RootElement;
        var id = root.GetProperty("id").GetString() ?? string.Empty;
        string? initPoint = null;
        if (root.TryGetProperty("init_point", out var init) && init.ValueKind != JsonValueKind.Null)
        {
            initPoint = init.GetString();
        }
        if (string.IsNullOrEmpty(initPoint) && root.TryGetProperty("sandbox_init_point", out var sandbox) && sandbox.ValueKind != JsonValueKind.Null)
        {
            initPoint = sandbox.GetString();
        }

        if (string.IsNullOrEmpty(initPoint))
        {
            throw new InvalidOperationException("MercadoPago response did not contain a valid init_point or sandbox_init_point.");
        }

        return new MercadoPagoCheckoutPreference(id, new Uri(initPoint));
    }

    public async Task<MercadoPagoPaymentDetails> GetPaymentAsync(string paymentId, CancellationToken ct)
    {
        EnsureConfigured();

        using var response = await _httpClient.GetAsync($"/v1/payments/{Uri.EscapeDataString(paymentId)}", ct);
        if (!response.IsSuccessStatusCode)
        {
            throw await CreateProviderExceptionAsync("get payment", response, ct);
        }

        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync(ct));
        var root = document.RootElement;
        var externalReference = root.GetProperty("external_reference").GetString() ?? string.Empty;
        var status = root.GetProperty("status").GetString() ?? string.Empty;
        var subscriptionId = root.TryGetProperty("preapproval_id", out var preapproval) ? preapproval.GetString() : null;

        var dateApproved = TryParseProviderUtcDate(root, "date_approved");
        var providerTransitionAt = TryParseProviderUtcDate(root, "date_last_updated");

        if (!Guid.TryParse(externalReference, out var hogarId))
        {
            throw new InvalidOperationException($"Invalid HogarId in payment external_reference: '{externalReference}'");
        }

        return new MercadoPagoPaymentDetails(paymentId, hogarId, status, subscriptionId, dateApproved, providerTransitionAt);
    }

    private static DateTime? TryParseProviderUtcDate(JsonElement root, string propertyName)
    {
        if (!root.TryGetProperty(propertyName, out var value)
            || value.ValueKind is JsonValueKind.Null or not JsonValueKind.String)
        {
            return null;
        }

        var dateValue = value.GetString();
        if (string.IsNullOrWhiteSpace(dateValue)
            || !Regex.IsMatch(dateValue, @"(?:Z|[+-]\d{2}:\d{2})$", RegexOptions.CultureInvariant)
            || !DateTimeOffset.TryParse(
                dateValue,
                System.Globalization.CultureInfo.InvariantCulture,
                System.Globalization.DateTimeStyles.AllowWhiteSpaces,
                out var parsed))
        {
            return null;
        }

        return parsed.UtcDateTime;
    }

    private void EnsureConfigured()
    {
        if (string.IsNullOrWhiteSpace(_options.AccessToken))
        {
            throw new InvalidOperationException("MercadoPago:AccessToken is required for payment operations.");
        }
    }

    private bool HasSuitableBackUrls()
        => IsSuitableBackUrl(_options.SuccessUrl)
            && IsSuitableBackUrl(_options.PendingUrl)
            && IsSuitableBackUrl(_options.FailureUrl);

    private static bool IsSuitableBackUrl(string? value)
    {
        if (string.IsNullOrWhiteSpace(value) ||
            !Uri.TryCreate(value, UriKind.Absolute, out var uri))
        {
            return false;
        }

        return uri.Scheme.Equals(Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase) &&
               !uri.IsLoopback &&
               !uri.Host.Equals("localhost", StringComparison.OrdinalIgnoreCase);
    }

    private static async Task<HttpRequestException> CreateProviderExceptionAsync(
        string operation,
        HttpResponseMessage response,
        CancellationToken ct)
    {
        string? providerErrorCode = null;
        try
        {
            using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync(ct));
            providerErrorCode = TryGetProviderErrorCode(document.RootElement);
        }
        catch (JsonException)
        {
            // Provider error bodies are untrusted. Do not include them in exceptions or logs.
        }

        var safeCode = providerErrorCode ?? "unavailable";
        return new HttpRequestException(
            $"Mercado Pago {operation} failed. ProviderStatusCode: {(int)response.StatusCode}. ProviderErrorCode: {safeCode}.",
            inner: null,
            response.StatusCode);
    }

    private static string? TryGetProviderErrorCode(JsonElement root)
    {
        if (root.ValueKind != JsonValueKind.Object)
        {
            return null;
        }

        foreach (var propertyName in new[] { "error", "code" })
        {
            if (!root.TryGetProperty(propertyName, out var value)
                || value.ValueKind != JsonValueKind.String)
            {
                continue;
            }

            var code = value.GetString();
            if (!string.IsNullOrWhiteSpace(code)
                && code.Length <= 64
                && Regex.IsMatch(code, "^[A-Za-z0-9._-]+$", RegexOptions.CultureInvariant))
            {
                return code;
            }
        }

        return null;
    }

    public static void ConfigureClient(HttpClient client, MercadoPagoOptions options)
    {
        client.BaseAddress = new Uri(options.ApiBaseUrl);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", options.AccessToken);
        client.Timeout = TimeSpan.FromSeconds(15);
    }
}
