using Microsoft.Extensions.DependencyInjection;
using Nido.Application.CatalogoElectrodomesticos.UploadCatalogImage;
using Nido.Application.Electrodomesticos.UploadElectrodomesticoImage;

namespace Nido.Application.Electrodomesticos;

public static class DependencyInjection
{
    public static IServiceCollection AddElectrodomesticosModule(this IServiceCollection services)
    {
        services.AddScoped<CreateElectrodomesticoHandler>();
        services.AddScoped<GetElectrodomesticosHandler>();
        services.AddScoped<GetElectrodomesticosCatalogoHandler>();
        services.AddScoped<UploadElectrodomesticoImageHandler>();
        services.AddScoped<UploadCatalogImageHandler>();
        return services;
    }
}
