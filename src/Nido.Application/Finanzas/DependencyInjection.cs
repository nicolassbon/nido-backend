using Microsoft.Extensions.DependencyInjection;

namespace Nido.Application.Finanzas;

public static class DependencyInjection
{
    public static IServiceCollection AddFinanzasModule(this IServiceCollection services)
    {
        services.AddScoped<CreateGastoHandler>();
        services.AddScoped<GetGastosHandler>();
        services.AddScoped<GetBalanceHandler>();
        services.AddScoped<ToggleModoAhorroHandler>();
        services.AddScoped<GetRecomendacionesHandler>();
        services.AddScoped<CreateFacturaHandler>();
        services.AddScoped<GetFacturasHandler>();
        services.AddScoped<MarcarPagadaHandler>();
        services.AddScoped<DeleteFacturaHandler>();
        services.AddScoped<GetSavingsPotencialHandler>();
        services.AddScoped<GetInsightsHandler>();
        services.AddScoped<GetAlacenaOportunidadesHandler>();
        return services;
    }
}
