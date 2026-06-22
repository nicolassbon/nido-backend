namespace Nido.Application.Telegram.Client;
public abstract record TelegramSendResult
{
    private TelegramSendResult() { }

    public sealed record Success(TelegramMessageSent Message) : TelegramSendResult;

    public sealed record Error(TelegramSendError Value) : TelegramSendResult;
}

public sealed record TelegramMessageSent(long MessageId);

public abstract record TelegramSendError
{
    public string Code { get; }
    public string Description { get; }

    protected TelegramSendError(string code, string description)
    {
        Code = code;
        Description = description;
    }
}

public sealed record TelegramRateLimitError(string Description, int RetryAfter)
    : TelegramSendError("TELEGRAM_RATE_LIMIT", Description);

public sealed record TelegramPermanentError(string Description)
    : TelegramSendError("TELEGRAM_PERMANENT_ERROR", Description);

public sealed record TelegramTransientError(string Description)
    : TelegramSendError("TELEGRAM_TRANSIENT_ERROR", Description);

public sealed record TelegramValidationError(string Description)
    : TelegramSendError("TELEGRAM_VALIDATION_ERROR", Description);
