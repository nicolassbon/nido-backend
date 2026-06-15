using Microsoft.Extensions.DependencyInjection;

namespace Nido.Application.Hogares;

public static class DependencyInjection
{
    public static IServiceCollection AddHogaresModule(this IServiceCollection services)
    {
        services.AddScoped<InvitarIntegranteHandler>();
        services.AddScoped<AceptarInvitacionHandler>();
        services.AddScoped<GetInvitacionPreviewHandler>();
        services.AddScoped<GetMiembrosHandler>();
        services.AddScoped<RemoveMiembroHandler>();
        return services;
    }
}
