using Microsoft.Extensions.DependencyInjection;

namespace Nido.Application.Electrodomesticos;

public static class DependencyInjection
{
    public static IServiceCollection AddElectrodomesticosModule(this IServiceCollection services)
    {
        services.AddScoped<CreateElectrodomesticoHandler>();
        services.AddScoped<GetElectrodomesticosHandler>();
        services.AddScoped<GetElectrodomesticosCatalogoHandler>();
        return services;
    }
}
