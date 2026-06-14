using Microsoft.Extensions.DependencyInjection;

namespace Nido.Application.Insights;

public static class DependencyInjection
{
    public static IServiceCollection AddInsightsModule(this IServiceCollection services)
    {
        services.AddScoped<GetInsightsHogarHandler>();
        return services;
    }
}
