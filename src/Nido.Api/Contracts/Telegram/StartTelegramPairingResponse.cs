namespace Nido.Api.Contracts.Telegram;

public sealed record StartTelegramPairingResponse(string DeepLinkUrl, DateTime ExpiresAt);
