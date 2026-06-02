using Microsoft.Extensions.DependencyInjection;

namespace Nido.Application.Onboarding;

public static class DependencyInjection
{
    public static IServiceCollection AddOnboardingModule(this IServiceCollection services)
    {
        services.AddScoped<SaveHouseholdStepHandler>();
        services.AddScoped<SaveEquipmentStepHandler>();
        services.AddScoped<SaveWellnessStepHandler>();
        services.AddScoped<GetPreferenciasAlimentariasHandler>();
        services.AddScoped<GetAlergiasHandler>();
        services.AddScoped<GetMetasHandler>();
        return services;
    }
}
