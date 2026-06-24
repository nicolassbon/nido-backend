using System.Security.Cryptography;
using System.Text;
using Nido.Application.Telegram.Pairing;

namespace Nido.Infrastructure.Telegram.Pairing;

public sealed class TelegramPairingTokenHasher : ITelegramPairingTokenHasher
{
    public string Hash(string token)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(token);

        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(token));
        return Convert.ToHexStringLower(bytes);
    }
}
