using Microsoft.Extensions.DependencyInjection;
using Nido.Application.Productos.UploadProductImage;


namespace Nido.Application.Productos;


public static class DependencyInjection
{
    public static IServiceCollection AddProductosModule(this IServiceCollection services)
    {
        services.AddScoped<GetProductByBarcodeHandler>();
        services.AddScoped<CreateStockHomeHandler>();
        services.AddScoped<GetProductManualHandler>();
        services.AddScoped<SearchProductosHandler>();
        services.AddScoped<LookupExternalProductoHandler>();
        services.AddScoped<UploadProductImageHandler>();
        return services;
    }
}
