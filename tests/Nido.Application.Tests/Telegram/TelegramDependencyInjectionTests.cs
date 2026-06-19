using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Nido.Application.Telegram;
using Nido.Application.Telegram.Authorization;
using Nido.Application.Telegram.Pairing;
using Xunit;

namespace Nido.Application.Tests.Telegram;

public sealed class TelegramDependencyInjectionTests
{
    [Fact]
    public void AddTelegramModule_RegistersPairingPorts()
    {
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>())
            .Build();

        services.AddTelegramModule(configuration);

        using var provider = services.BuildServiceProvider();
        Assert.NotNull(provider.GetRequiredService<ITelegramHogarAccess>());
        Assert.NotNull(provider.GetRequiredService<ITelegramPairingRepository>());
        Assert.NotNull(provider.GetRequiredService<ITelegramPairingTokenHasher>());
        Assert.NotNull(provider.GetRequiredService<ITelegramPairingRateLimiter>());
    }
}
