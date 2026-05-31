using Microsoft.Extensions.DependencyInjection;

namespace Nido.Application.Preferencias;

public static class DependencyInjection
{
    public static IServiceCollection AddPreferenciasModule(this IServiceCollection services)
    {
        services.AddScoped<GetUserPreferencesHandler>();
        services.AddScoped<UpdateUserPreferencesHandler>();
        return services;
    }
}
