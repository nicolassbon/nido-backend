using Microsoft.Extensions.DependencyInjection;

namespace Nido.Application.ListaCompras;

public static class DependencyInjection
{
    public static IServiceCollection AddListaComprasModule(this IServiceCollection services)
    {
        services.AddScoped<GetListaComprasHandler>();
        services.AddScoped<GetListaComprasHistorialHandler>();
        services.AddScoped<AddListaCompraGroupHandler>();
        services.AddScoped<AddListaCompraItemHandler>();
        services.AddScoped<MarkListaCompraItemCompradoHandler>();
        services.AddScoped<MarkListaCompraItemCompradoByNameHandler>();
        services.AddScoped<RemoveListaCompraItemHandler>();
        services.AddScoped<ClearListaComprasHandler>();
        return services;
    }
}

