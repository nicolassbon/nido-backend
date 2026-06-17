using Nido.Application.Telegram.Client;

namespace Nido.Application.Tests.Telegram;

public sealed class TelegramClientContractTests
{
    [Fact]
    public void TelegramSendResult_Success_HasMessageId()
    {
        var result = new TelegramSendResult.Success(new TelegramMessageSent(42L));

        Assert.Equal(42L, result.Message.MessageId);
    }

    [Fact]
    public void TelegramSendResult_Error_HasCodeAndDescription()
    {
        var error = new TelegramPermanentError("chat not found");
        var result = new TelegramSendResult.Error(error);

        Assert.Equal("TELEGRAM_PERMANENT_ERROR", result.Value.Code);
        Assert.Equal("chat not found", result.Value.Description);
    }

    [Fact]
    public void TelegramRateLimitError_CarriesRetryAfter()
    {
        var error = new TelegramRateLimitError("Too many requests", 30);

        Assert.Equal("TELEGRAM_RATE_LIMIT", error.Code);
        Assert.Equal(30, error.RetryAfter);
    }

    [Fact]
    public void TelegramTransientError_IsTransientCode()
    {
        var error = new TelegramTransientError("timeout");

        Assert.Equal("TELEGRAM_TRANSIENT_ERROR", error.Code);
    }

    [Fact]
    public void TelegramValidationError_IsValidationCode()
    {
        var error = new TelegramValidationError("chatId invalid");

        Assert.Equal("TELEGRAM_VALIDATION_ERROR", error.Code);
    }

    [Fact]
    public void TelegramInlineKeyboardMarkup_SupportsNestedRows()
    {
        var markup = new TelegramInlineKeyboardMarkup(new[]
        {
            new[] { new TelegramInlineKeyboardButton("A", "a") },
            new[] { new TelegramInlineKeyboardButton("B", "b"), new TelegramInlineKeyboardButton("C", "c") }
        });

        Assert.Equal(2, markup.InlineKeyboard.Count);
        Assert.Single(markup.InlineKeyboard[0]);
        Assert.Equal(2, markup.InlineKeyboard[1].Count);
    }
}
