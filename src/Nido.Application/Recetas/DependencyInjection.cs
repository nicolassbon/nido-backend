using Microsoft.Extensions.DependencyInjection;

namespace Nido.Application.Recetas;

public static class DependencyInjection
{
    public static IServiceCollection AddRecetasModule(this IServiceCollection services)
    {
        services.AddScoped<GetRecetasHandler>();
        return services;
    }
}
