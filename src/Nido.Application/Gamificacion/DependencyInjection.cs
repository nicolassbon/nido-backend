using Microsoft.Extensions.DependencyInjection;

namespace Nido.Application.Gamificacion;

public static class DependencyInjection
{
    public static IServiceCollection AddGamificacionModule(this IServiceCollection services)
    {
        services.AddOptions<GamificationOptions>()
            .BindConfiguration(GamificationOptions.SectionName);

        services.AddScoped<IGamificationRulesService, GamificationRulesService>();
        services.AddScoped<IGamificationUnlockMaterializer, GamificationUnlockMaterializer>();
        services.AddScoped<GetGamificationProgressHandler>();

        return services;
    }
}
