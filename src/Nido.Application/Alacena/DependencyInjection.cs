using Microsoft.Extensions.DependencyInjection;

namespace Nido.Application.Alacena;

public static class DependencyInjection
{
    public static IServiceCollection AddAlacenaModule(this IServiceCollection services)
    {
        services.AddScoped<GetStockItemsHandler>();
        services.AddScoped<CreateStockItemHandler>();
        services.AddScoped<UpdateStockItemHandler>();
        services.AddScoped<DeleteStockItemHandler>();
        return services;
    }
}
