using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Nido.Infrastructure.Persistence;
using Nido.Domain.Electrodomesticos;
using Nido.Infrastructure.Electrodomesticos;
using Nido.Application.Electrodomesticos;
using Nido.Application.Auth;
using Nido.Application.Onboarding;
using Nido.Infrastructure.Auth;
using Nido.Infrastructure.Onboarding;

namespace Nido.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddNidoInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? configuration.GetConnectionString("Nido");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException("Missing required configuration: ConnectionStrings:DefaultConnection (or legacy ConnectionStrings:Nido)");
        }

        services.AddDbContext<NidoDbContext>(options =>
            options.UseNpgsql(connectionString));

        services.AddScoped<IElectrodomesticoRepository, ElectrodomesticoRepository>();
        services.AddScoped<GetElectrodomesticosHandler>();
        services.AddScoped<IAuthRepository, AuthRepository>();
        services.AddScoped<IPasswordHasher, BcryptPasswordHasher>();
        services.AddScoped<IJwtTokenService, JwtTokenService>();
        services.AddScoped<IOnboardingRepository, OnboardingRepository>();

        return services;
    }
}
