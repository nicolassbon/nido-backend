using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Nido.Infrastructure.Persistence;
using Nido.Domain.Electrodomesticos;
using Nido.Infrastructure.Electrodomesticos;
using Nido.Application.Auth.Interfaces;
using Nido.Application.Onboarding;
using Nido.Application.Hogares;
using Nido.Infrastructure.Auth;
using Nido.Infrastructure.Onboarding;
using Nido.Infrastructure.Hogares;
using Nido.Infrastructure.Email;
using Nido.Application.Alacena;
using Nido.Application.Common.Notifications;
using Nido.Application.Common.ProfileImages;
using Nido.Application.Productos;
using Nido.Application.Preferencias;
using Nido.Application.Recetas;
using Nido.Infrastructure.Alacena;
using Nido.Infrastructure.Productos;
using Nido.Application.UsuariosPerfil;
using Nido.Infrastructure.UsuariosPerfil;
using Nido.Infrastructure.ProfileImages;
using Nido.Infrastructure.Preferencias;
using Nido.Infrastructure.Recetas;
using Nido.Infrastructure.StockHogar;
using Resend;

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
        services.AddScoped<IHogarRepository, HogarRepository>();
        services.AddOptions();
        services.AddHttpClient<ResendClient>();
        services.Configure<ResendClientOptions>(options =>
        {
            options.ApiToken = configuration["RESEND_API_KEY"] ?? string.Empty;
            options.ThrowExceptions = true;
        });
        services.AddTransient<IResend, ResendClient>();
        services.AddScoped<IEmailService, ResendEmailService>();
        services.AddScoped<IAlacenaRepository, AlacenaRepository>();
        services.AddScoped<IProductoRepository, ProductoRepository>();
        services.AddScoped<IUsuarioRepository, UsuarioRepository>();
        services.AddScoped<IRecetaRepository, RecetaRepository>();
        services.AddOptions<ProfileImageOptions>().Bind(configuration.GetSection(ProfileImageOptions.SectionName));
        services.AddScoped<IProfileImageProcessor, ImageSharpProfileImageProcessor>();
        services.AddScoped<IProfileImageStorage, LocalProfileImageStorage>();
        services.AddScoped<IProfileImagePublicUrlResolver, ConfigurableProfileImagePublicUrlResolver>();
        services.AddScoped<IUserPreferencesRepository, UserPreferencesRepository>();

        // ── Lookup externo de productos por barcode ────────────────────────
        // Pipeline:
        //   IExternalProductLookupService
        //     → CachedExternalProductLookupService   (decorator: cache en memoria)
        //         → OpenFoodFactsLookupService        (consulta OFF + UPC Item DB)
        services.AddOptions<ExternalLookupOptions>()
            .Bind(configuration.GetSection(ExternalLookupOptions.SectionName));
        services.AddMemoryCache();
        services.AddSingleton<ProductCategoryMapper>();
        services.AddHttpClient<OpenFoodFactsLookupService>((sp, client) =>
        {
            var opts = sp.GetRequiredService<IOptions<ExternalLookupOptions>>().Value;
            client.Timeout = TimeSpan.FromSeconds(opts.TimeoutSeconds);
            client.DefaultRequestHeaders.UserAgent.ParseAdd(opts.UserAgent);
        });
        services.AddScoped<IExternalProductLookupService>(sp =>
            new CachedExternalProductLookupService(
                sp.GetRequiredService<OpenFoodFactsLookupService>(),
                sp.GetRequiredService<IMemoryCache>(),
                sp.GetRequiredService<IOptions<ExternalLookupOptions>>()));

        return services;
    }
}
