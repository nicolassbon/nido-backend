using Microsoft.Extensions.DependencyInjection;

namespace Nido.Application.Estadisticas;

public static class DependencyInjection
{
    public static IServiceCollection AddEstadisticasModule(this IServiceCollection services)
    {
        services.AddScoped<GetEstadisticasConsumoHandler>();
        return services;
    }
}
