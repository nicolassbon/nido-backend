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
