using Nido.Application.Telegram.Authorization;
using Nido.Application.Telegram.Menu;
using Nido.Infrastructure.Telegram.Menu;
using Xunit;

namespace Nido.Infrastructure.Tests.Telegram;

public sealed class TelegramMenuInfrastructureTests
{
    [Fact]
    public void Registry_GetDefaultMenu_ReturnsSixNumberedOptions()
    {
        var registry = new InMemoryTelegramMenuRegistry();

        var menu = registry.GetDefaultMenu();

        Assert.Equal("main-menu", menu.Id);
        Assert.Collection(
            menu.Options,
            option => Assert.Equal("1", option.Key),
            option => Assert.Equal("2", option.Key),
            option => Assert.Equal("3", option.Key),
            option => Assert.Equal("4", option.Key),
            option => Assert.Equal("5", option.Key),
            option => Assert.Equal("6", option.Key));
    }

    [Theory]
    [InlineData("2")]
    [InlineData("3")]
    [InlineData("4")]
    [InlineData("5")]
    [InlineData("6")]
    public async Task Provider_SelectAsync_ForStubOptions_ReturnsPlaceholder(string optionKey)
    {
        var provider = new TelegramMenuProvider();
        var link = new TelegramChatLinkSnapshot(10, Guid.NewGuid(), Guid.NewGuid(), DateTime.UtcNow, null);

        var result = await provider.SelectAsync("main-menu", optionKey, link, CancellationToken.None);

        Assert.True(result.Handled);
        Assert.Contains(optionKey, result.Text, StringComparison.Ordinal);
        Assert.False(result.ShouldClearState);
        Assert.Equal("main-menu", result.NextMenuId);
    }
}
