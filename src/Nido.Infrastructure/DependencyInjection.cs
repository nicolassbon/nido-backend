using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Nido.Infrastructure.Persistence;
using Nido.Domain.Electrodomesticos;
using Nido.Infrastructure.Electrodomesticos;
using Nido.Application.Electrodomesticos;
using Nido.Application.Auth;
using Nido.Application.Onboarding;
using Nido.Application.Hogares;
using Nido.Infrastructure.Auth;
using Nido.Infrastructure.Onboarding;
using Nido.Infrastructure.Hogares;
using Nido.Infrastructure.Email;
using Nido.Application.Alacena;
using Nido.Application.Common.ProfileImages;
using Nido.Application.Productos;
using Nido.Application.Preferencias;
using Nido.Infrastructure.Alacena;
using Nido.Infrastructure.Productos;
using Nido.Infrastructure.ProfileImages;
using Nido.Infrastructure.Preferencias;
using Nido.Domain.Productos;
using Nido.Infrastructure.StockHogar;
using Nido.Domain.StockHogar;

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
        services.AddScoped<IAuthRepository, AuthRepository>();
        services.AddScoped<IPasswordHasher, BcryptPasswordHasher>();
        services.AddScoped<IProductRepository, ProductRepository>();
        services.AddScoped<IProductManualRepository, ProductRepository>();
        services.AddScoped<IStockHogarRepository, StockHogarRepository>();
        services.AddScoped<IJwtTokenService, JwtTokenService>();
        services.AddOptions<GoogleOptions>()
            .Bind(configuration.GetSection(GoogleOptions.SectionName))
            .Validate(options => !string.IsNullOrWhiteSpace(options.ClientId), "Google:ClientId is required.")
            .ValidateOnStart();
        services.AddScoped<IGoogleTokenValidator, GoogleTokenValidator>();
        services.AddScoped<IOnboardingRepository, OnboardingRepository>();
        services.AddScoped<IInvitacionRepository, InvitacionRepository>();
        services.AddScoped<IEmailService, SmtpEmailService>();
        services.AddScoped<IAlacenaRepository, AlacenaRepository>();
        services.AddScoped<IProductoRepository, ProductoRepository>();
        services.AddOptions<ProfileImageOptions>().Bind(configuration.GetSection(ProfileImageOptions.SectionName));
        services.AddScoped<IProfileImageProcessor, ImageSharpProfileImageProcessor>();
        services.AddScoped<IProfileImageStorage, LocalProfileImageStorage>();
        services.AddScoped<IProfileImagePublicUrlResolver, ConfigurableProfileImagePublicUrlResolver>();
        services.AddScoped<IUserPreferencesRepository, UserPreferencesRepository>();

        return services;
    }
}
