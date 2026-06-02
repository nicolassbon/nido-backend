using Microsoft.Extensions.DependencyInjection;

namespace Nido.Application.Recetas;

public static class DependencyInjection
{
    public static IServiceCollection AddRecetasModule(this IServiceCollection services)
    {
        services.AddScoped<GetRecetasHandler>();
        services.AddScoped<GetRecetaByIdHandler>();
        services.AddScoped<CocinarRecetaHandler>();
        return services;
    }
}
