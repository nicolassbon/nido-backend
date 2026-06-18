using Microsoft.Extensions.DependencyInjection;

namespace Nido.Application.Notificaciones;

public static class DependencyInjection
{
    public static IServiceCollection AddNotificacionesModule(this IServiceCollection services)
    {
        services.AddScoped<GetNotificationsHandler>();
        services.AddScoped<MarkNotificationAsReadHandler>();
        services.AddScoped<MarkAllNotificationsAsReadHandler>();
        services.AddScoped<DeleteNotificationHandler>();
        services.AddScoped<SubscribePushHandler>();
        return services;
    }
}
