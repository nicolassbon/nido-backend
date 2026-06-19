using Nido.Domain.Exceptions;

namespace Nido.Application.Telegram.Exceptions;

public sealed class TelegramHogarAccessDeniedException : NidoException
{
    public TelegramHogarAccessDeniedException()
        : base("TELEGRAM_HOGAR_ACCESS_DENIED",
               "The user linked to this Telegram chat is no longer a member of the household.")
    { }
}

public sealed class TelegramChatNotLinkedException : NidoException
{
    public TelegramChatNotLinkedException() : base("TELEGRAM_CHAT_NOT_LINKED", "This Telegram chat is not linked to a Nido household.") { }
}

public sealed class TelegramUpdateAlreadyProcessedException : NidoException
{
    public TelegramUpdateAlreadyProcessedException(long updateId)
        : base("TELEGRAM_UPDATE_ALREADY_PROCESSED", $"Telegram update {updateId} has already been processed.")
    { }
}

public sealed class TelegramConfigurationException : NidoException
{
    public TelegramConfigurationException(string detail)
        : base("TELEGRAM_CONFIGURATION", detail) { }
}

public sealed class TelegramPairingTokenNotFoundException : NidoException
{
    public TelegramPairingTokenNotFoundException() : base("TELEGRAM_PAIRING_TOKEN_NOT_FOUND", "The Telegram pairing token was not found. Please request a new deep link.") { }
}

public sealed class TelegramPairingTokenAlreadyConsumedException : NidoException
{
    public TelegramPairingTokenAlreadyConsumedException() : base("TELEGRAM_PAIRING_TOKEN_ALREADY_CONSUMED", "This pairing token has already been consumed.") { }
}

public sealed class TelegramPairingTokenExpiredException : NidoException
{
    public TelegramPairingTokenExpiredException() : base("TELEGRAM_PAIRING_TOKEN_EXPIRED", "This pairing token has expired. Please request a new one.") { }
}

public sealed class TelegramPairingTokenRevokedException : NidoException
{
    public TelegramPairingTokenRevokedException() : base("TELEGRAM_PAIRING_TOKEN_REVOKED", "This pairing token was revoked. Please request a new one.") { }
}

public sealed class TelegramPairingRateLimitExceededException : NidoException
{
    public TelegramPairingRateLimitExceededException() : base("TELEGRAM_PAIRING_RATE_LIMIT_EXCEEDED", "Telegram pairing is temporarily rate limited. Please try again shortly.") { }
}

public sealed class TelegramPairingCodeExpiredException : NidoException
{
    public TelegramPairingCodeExpiredException() : base("TELEGRAM_PAIRING_CODE_EXPIRED", "This pairing code has expired. Please request a new one.") { }
}

public sealed class TelegramPairingCodeRevokedException : NidoException
{
    public TelegramPairingCodeRevokedException() : base("TELEGRAM_PAIRING_CODE_REVOKED", "This pairing code was revoked after too many wrong attempts.") { }
}

public sealed class TelegramTareaNotAssignedToUserException : NidoException
{
    public TelegramTareaNotAssignedToUserException() : base("TELEGRAM_TAREA_NOT_ASSIGNED", "This task is not assigned to you; only the assigned user can complete it from Telegram.") { }
}
