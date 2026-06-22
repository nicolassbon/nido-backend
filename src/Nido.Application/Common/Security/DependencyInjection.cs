using Microsoft.Extensions.DependencyInjection;

namespace Nido.Application.Common.Security;

public static class DependencyInjection
{
    public static IServiceCollection AddCommonSecurityModule(this IServiceCollection services)
    {
        services.AddScoped<IHouseholdMembershipService, HouseholdMembershipService>();
        return services;
    }
}
