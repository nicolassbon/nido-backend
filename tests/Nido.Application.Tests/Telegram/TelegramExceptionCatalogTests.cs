using System;
using System.Linq;
using System.Reflection;
using Nido.Application.Telegram.Exceptions;
using Nido.Domain.Exceptions;
using Xunit;

namespace Nido.Application.Tests.Telegram;

public sealed class TelegramExceptionCatalogTests
{
    [Fact]
    public void AllTelegramExceptionTypes_ArePresent()
    {
        var assembly = typeof(TelegramHogarAccessDeniedException).Assembly;
        var namespaceName = typeof(TelegramHogarAccessDeniedException).Namespace!;

        var expected = new[]
        {
            "TelegramHogarAccessDeniedException",
            "TelegramChatNotLinkedException",
            "TelegramUpdateAlreadyProcessedException",
            "TelegramConfigurationException",
            "TelegramPairingTokenNotFoundException",
            "TelegramPairingTokenAlreadyConsumedException",
            "TelegramPairingTokenExpiredException",
            "TelegramPairingTokenRevokedException",
            "TelegramPairingRateLimitExceededException",
            "TelegramPairingCodeNotFoundException",
            "TelegramPairingCodeExpiredException",
            "TelegramPairingCodeRevokedException",
            "TelegramTareaNotAssignedToUserException"
        };

        var actual = assembly
            .GetTypes()
            .Where(t => t.Namespace == namespaceName && !t.IsAbstract)
            .Select(t => t.Name)
            .ToHashSet();

        foreach (var type in expected)
        {
            Assert.Contains(type, actual);
        }
    }

    [Fact]
    public void AllTelegramExceptions_InheritFromNidoException()
    {
        var assembly = typeof(TelegramHogarAccessDeniedException).Assembly;
        var namespaceName = typeof(TelegramHogarAccessDeniedException).Namespace!;

        var telegramExceptions = assembly
            .GetTypes()
            .Where(t => t.Namespace == namespaceName && !t.IsAbstract);

        Assert.NotEmpty(telegramExceptions);

        foreach (var t in telegramExceptions)
        {
            Assert.True(
                typeof(NidoException).IsAssignableFrom(t),
                $"{t.FullName} must inherit from NidoException.");
        }
    }

    [Fact]
    public void AllTelegramExceptions_AreSealed()
    {
        var assembly = typeof(TelegramHogarAccessDeniedException).Assembly;
        var namespaceName = typeof(TelegramHogarAccessDeniedException).Namespace!;

        var telegramExceptions = assembly
            .GetTypes()
            .Where(t => t.Namespace == namespaceName && !t.IsAbstract);

        foreach (var t in telegramExceptions)
        {
            Assert.True(t.IsSealed, $"{t.FullName} must be sealed.");
        }
    }

    [Fact]
    public void TelegramHogarAccessDeniedException_CarriesStableCode()
    {
        var ex = new TelegramHogarAccessDeniedException();

        Assert.Equal("TELEGRAM_HOGAR_ACCESS_DENIED", ex.Code);
    }

    [Fact]
    public void TelegramConfigurationException_CarriesProvidedDetailInMessage()
    {
        var ex = new TelegramConfigurationException("BotToken is required when the webhook is registered.");

        Assert.Equal("TELEGRAM_CONFIGURATION", ex.Code);
        Assert.Contains("BotToken is required", ex.Message);
    }

    [Fact]
    public void TelegramPairingTokenNotFoundException_CarriesStableCode()
    {
        var ex = new TelegramPairingTokenNotFoundException();

        Assert.Equal("TELEGRAM_PAIRING_TOKEN_NOT_FOUND", ex.Code);
        Assert.NotEmpty(ex.Message);
    }

    [Fact]
    public void TelegramChatNotLinkedException_CarriesStableCode()
    {
        var ex = new TelegramChatNotLinkedException();

        Assert.Equal("TELEGRAM_CHAT_NOT_LINKED", ex.Code);
        Assert.NotEmpty(ex.Message);
    }

    [Fact]
    public void TelegramPairingCodeNotFoundException_CarriesStableCode()
    {
        var ex = new TelegramPairingCodeNotFoundException();

        Assert.Equal("TELEGRAM_PAIRING_CODE_NOT_FOUND", ex.Code);
        Assert.NotEmpty(ex.Message);
    }

    [Fact]
    public void TelegramUpdateAlreadyProcessedException_IncludesUpdateIdInMessage()
    {
        const long updateId = 42_195L;

        var ex = new TelegramUpdateAlreadyProcessedException(updateId);

        Assert.Equal("TELEGRAM_UPDATE_ALREADY_PROCESSED", ex.Code);
        Assert.Contains(updateId.ToString(), ex.Message);
    }

    [Theory]
    [InlineData(typeof(TelegramPairingTokenExpiredException), "TELEGRAM_PAIRING_TOKEN_EXPIRED")]
    [InlineData(typeof(TelegramPairingTokenRevokedException), "TELEGRAM_PAIRING_TOKEN_REVOKED")]
    [InlineData(typeof(TelegramPairingTokenAlreadyConsumedException), "TELEGRAM_PAIRING_TOKEN_ALREADY_CONSUMED")]
    [InlineData(typeof(TelegramPairingRateLimitExceededException), "TELEGRAM_PAIRING_RATE_LIMIT_EXCEEDED")]
    [InlineData(typeof(TelegramPairingCodeExpiredException), "TELEGRAM_PAIRING_CODE_EXPIRED")]
    [InlineData(typeof(TelegramPairingCodeRevokedException), "TELEGRAM_PAIRING_CODE_REVOKED")]
    [InlineData(typeof(TelegramTareaNotAssignedToUserException), "TELEGRAM_TAREA_NOT_ASSIGNED")]
    public void TelegramPairingExceptions_CarryStableCodeAndMessage(Type exceptionType, string expectedCode)
    {
        var ex = (NidoException)Activator.CreateInstance(exceptionType, nonPublic: true)!;

        Assert.Equal(expectedCode, ex.Code);
        Assert.NotEmpty(ex.Message);
    }
}
