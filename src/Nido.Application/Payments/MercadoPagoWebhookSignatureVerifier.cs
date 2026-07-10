using System.Security.Cryptography;
using System.Text;

namespace Nido.Application.Payments;

public sealed class MercadoPagoWebhookSignatureVerifier
{
    public bool Verify(string dataId, string requestId, string signatureHeader, string secret)
    {
        if (string.IsNullOrWhiteSpace(dataId)
            || string.IsNullOrWhiteSpace(requestId)
            || string.IsNullOrWhiteSpace(signatureHeader)
            || string.IsNullOrWhiteSpace(secret))
        {
            return false;
        }

        var parts = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var segment in signatureHeader
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(p => p.Split('=', 2, StringSplitOptions.TrimEntries))
            .Where(p => p.Length == 2))
        {
            if (!parts.TryAdd(segment[0], segment[1]))
            {
                return false; // duplicate parameter key = malformed signature
            }
        }

        if (!parts.TryGetValue("ts", out var timestamp) || !parts.TryGetValue("v1", out var receivedHash))
        {
            return false;
        }

        var manifest = $"id:{dataId};request-id:{requestId};ts:{timestamp};";
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
        var expectedHash = Convert.ToHexString(hmac.ComputeHash(Encoding.UTF8.GetBytes(manifest))).ToLowerInvariant();

        var expectedBytes = Encoding.UTF8.GetBytes(expectedHash);
        var receivedBytes = Encoding.UTF8.GetBytes(receivedHash.ToLowerInvariant());

        if (expectedBytes.Length != receivedBytes.Length)
        {
            return false;
        }

        return CryptographicOperations.FixedTimeEquals(expectedBytes, receivedBytes);
    }
}
