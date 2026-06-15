using Microsoft.Extensions.DependencyInjection;

namespace Nido.Application.Recetas;

public static class DependencyInjection
{
    public static IServiceCollection AddRecetasModule(this IServiceCollection services)
    {
        services.AddScoped<GetRecetasHandler>();
        services.AddScoped<GetRecetaByIdHandler>();
        services.AddScoped<CocinarRecetaHandler>();
        services.AddScoped<UpsertResenaHandler>();
        services.AddScoped<GetResenasByRecetaHandler>();
        services.AddScoped<DeleteResenaHandler>();
        services.AddScoped<AddNotaRecetaHandler>();
        services.AddScoped<DeleteNotaRecetaHandler>();
        services.AddScoped<GetNotasByRecetaHandler>();
        return services;
    }
}
