using Microsoft.Extensions.DependencyInjection;

namespace Nido.Application.Tickets;

public static class DependencyInjection
{
    public static IServiceCollection AddTicketsModule(this IServiceCollection services)
    {
        services.AddScoped<ScanTicketHandler>();
        return services;
    }
}