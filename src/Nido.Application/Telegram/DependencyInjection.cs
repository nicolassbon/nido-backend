using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Nido.Application.Telegram.Authorization;

namespace Nido.Application.Telegram;

public static class DependencyInjection
{
    public static IServiceCollection AddTelegramModule(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<TelegramOptions>()
            .Bind(configuration.GetSection(TelegramOptions.SectionName));

        services.AddSingleton<TelegramHogarAccess>();
        return services;
    }
}
