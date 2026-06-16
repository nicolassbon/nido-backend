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
    public void AllEightExceptionTypes_ArePresent()
    {
        var assembly = typeof(TelegramHogarAccessDeniedException).Assembly;
        var namespaceName = typeof(TelegramHogarAccessDeniedException).Namespace!;

        var expected = new[]
        {
            "TelegramHogarAccessDeniedException",
            "TelegramChatNotLinkedException",
            "TelegramUpdateAlreadyProcessedException",
            "TelegramConfigurationException",
            "TelegramPairingTokenAlreadyConsumedException",
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
}
