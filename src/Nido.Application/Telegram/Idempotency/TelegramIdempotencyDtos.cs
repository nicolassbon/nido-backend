using System;

namespace Nido.Application.Telegram.Idempotency;

public sealed record TelegramUpdateIdempotencyResult(
    bool AlreadyProcessed,
    long UpdateId,
    string? UpdateHash,
    DateTime? ProcessedAt);
