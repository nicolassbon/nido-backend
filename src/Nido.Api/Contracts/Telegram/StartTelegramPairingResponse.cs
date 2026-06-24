namespace Nido.Api.Contracts.Telegram;

public sealed record StartTelegramPairingResponse(
    string DeepLinkUrl,
    string PairingCode,
    DateTime TokenExpiresAt,
    DateTime CodeExpiresAt)
{
    public DateTime ExpiresAt => CodeExpiresAt;
}
