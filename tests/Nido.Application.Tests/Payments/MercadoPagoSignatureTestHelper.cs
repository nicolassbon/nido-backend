using System.Security.Cryptography;
using System.Text;

namespace Nido.Application.Tests.Payments;

internal static class MercadoPagoSignatureTestHelper
{
    public static string BuildSignature(string dataId, string requestId, string timestamp, string secret)
    {
        var manifest = $"id:{dataId};request-id:{requestId};ts:{timestamp};";
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(manifest));
        return $"ts={timestamp},v1={Convert.ToHexString(hash).ToLowerInvariant()}";
    }
}
