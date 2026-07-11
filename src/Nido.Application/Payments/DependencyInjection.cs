using Microsoft.Extensions.DependencyInjection;

namespace Nido.Application.Payments;

public static class DependencyInjection
{
    public static IServiceCollection AddPaymentsModule(this IServiceCollection services)
    {
        services.AddScoped<EntitlementService>();
        services.AddScoped<IEntitlementService>(sp => sp.GetRequiredService<EntitlementService>());
        services.AddScoped<CreateCheckoutPreferenceHandler>();
        services.AddScoped<GetSubscriptionHandler>();
        services.AddScoped<SetDevelopmentEntitlementHandler>();
        services.AddScoped<ProcessWebhookHandler>();
        services.AddSingleton<MercadoPagoWebhookSignatureVerifier>();
        return services;
    }
}
