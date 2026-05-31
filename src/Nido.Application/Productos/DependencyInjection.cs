using Microsoft.Extensions.DependencyInjection;

namespace Nido.Application.Productos;

public static class DependencyInjection
{
    public static IServiceCollection AddProductosModule(this IServiceCollection services)
    {
        services.AddScoped<GetProductByBarcodeHandler>();
        services.AddScoped<CreateProductoHandler>();
        services.AddScoped<GetProductManualHandler>();
        return services;
    }
}
