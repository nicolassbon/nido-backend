using System;

namespace Nido.Application.Telegram.Pairing;

public sealed record TelegramPairingTokenResult(
    Guid Id,
    Guid HogarId,
    Guid UsuarioId,
    DateTime CreatedAt,
    DateTime ExpiresAt,
    DateTime? ConsumedAt,
    DateTime? RevokedAt,
    TelegramPairingStatus Status);

public sealed record TelegramPairingCodeResult(
    Guid Id,
    Guid HogarId,
    Guid UsuarioId,
    int AttemptCount,
    DateTime CreatedAt,
    DateTime ExpiresAt,
    DateTime? ConsumedAt,
    DateTime? RevokedAt,
    TelegramPairingStatus Status);

public sealed record ValidatePairingCodeRequest(
    long ChatId,
    string SubmittedCode);

public sealed record StartTelegramPairingCommand(
    Guid UsuarioId,
    Guid HogarId);

public sealed record StartTelegramPairingResult(
    string DeepLinkUrl,
    string PairingCode,
    DateTime TokenExpiresAt,
    DateTime CodeExpiresAt)
{
    public DateTime ExpiresAt => CodeExpiresAt;
}

public sealed record CompleteTelegramPairingCommand(
    long ChatId,
    string Token);

public sealed record CompleteTelegramPairingByCodeCommand(
    long ChatId,
    string Code);

public sealed record CompleteTelegramPairingResult(
    long ChatId,
    Guid HogarId,
    Guid UsuarioId,
    DateTime PairedAt);

public sealed record UnlinkTelegramChatCommand(long ChatId);

public sealed record UnlinkTelegramChatResult(
    long ChatId,
    Guid HogarId,
    Guid UsuarioId,
    DateTime UnpairedAt);
