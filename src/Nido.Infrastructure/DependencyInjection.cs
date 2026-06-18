using Amazon.Runtime;
using Amazon.S3;
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
using Nido.Application.CatalogoElectrodomesticos.UploadCatalogImage;
using Nido.Application.Common.Assets;
using Nido.Application.Common.Images;
using Nido.Application.Common.Notifications;
using Nido.Application.Common.ProfileImages;
using Nido.Application.Common.Storage;
using Nido.Application.Electrodomesticos.UploadElectrodomesticoImage;
using Nido.Application.Productos;
using Nido.Application.Preferencias;
using Nido.Application.Recetas;
using Nido.Application.Estadisticas;
using Nido.Infrastructure.Estadisticas;
using Nido.Infrastructure.Alacena;
using Nido.Infrastructure.Productos;
using Nido.Application.UsuariosPerfil;
using Nido.Infrastructure.UsuariosPerfil;
using Nido.Infrastructure.ProfileImages;
using Nido.Infrastructure.PublicAssets;
using Nido.Infrastructure.Images;
using Nido.Infrastructure.Preferencias;
using Nido.Infrastructure.Recetas;
using Nido.Infrastructure.StockHogar;
using Nido.Application.Finanzas;
using Nido.Infrastructure.Finanzas;
using Nido.Infrastructure.Storage;
using Nido.Application.Productos.UploadProductImage;
using Nido.Application.Recetas.UploadRecipeImage;
using Nido.Application.Tareas;
using Nido.Infrastructure.Tareas;
using Nido.Application.Notificaciones;
using Nido.Infrastructure.Notificaciones;
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
            options.UseNpgsql(connectionString)
                   .ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.RelationalEventId.PendingModelChangesWarning)));

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
        services.AddScoped<IResenaRecetaRepository, ResenaRecetaRepository>();
        services.AddScoped<INotaRecetaRepository, NotaRecetaRepository>();
        services.AddScoped<IRecetaRepository, RecetaRepository>();
        services.AddScoped<IEstadisticasRepository, EstadisticasRepository>();
        services.AddScoped<Nido.Application.Insights.IConsumoProductoRepository, Nido.Infrastructure.Insights.ConsumoProductoRepository>();
        services.AddOptions<ProfileImageOptions>().Bind(configuration.GetSection(ProfileImageOptions.SectionName));
        services.AddScoped<IProfileImageProcessor, ImageSharpProfileImageProcessor>();
        services.AddScoped<IProfileImagePublicUrlResolver, ConfigurableProfileImagePublicUrlResolver>();
        services.AddOptions<SpacesOptions>()
            .Bind(configuration.GetSection(SpacesOptions.SectionName))
            .Validate(options => !options.Enabled || options.HasUploadConfiguration(), "Spaces upload configuration is incomplete.")
            .ValidateOnStart();
        services.AddScoped<IAmazonS3>(sp =>
        {
            var options = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<SpacesOptions>>().Value;
            var config = new AmazonS3Config
            {
                ServiceURL = options.Endpoint,
                ForcePathStyle = false
            };

            return new AmazonS3Client(new BasicAWSCredentials(options.AccessKey, options.SecretKey), config);
        });
        services.AddScoped<IFileStorageService, SpacesS3Storage>();
        services.AddScoped<StorageKeyFactory>();
        services.AddScoped<IImageProcessingService, ImageSharpImageProcessingService>();
        services.AddScoped<IPublicAssetUrlResolver, SpacesPublicAssetUrlResolver>();
        services.AddScoped<IProductImageRepository, ProductoRepository>();
        services.AddScoped<IElectrodomesticoImageRepository, ElectrodomesticoRepository>();
        services.AddScoped<ICatalogImageRepository, ElectrodomesticoRepository>();
        services.AddScoped<IRecipeImageRepository, RecetaRepository>();
        services.AddScoped<IUserPreferencesRepository, UserPreferencesRepository>();
        services.AddScoped<IFinanzasRepository, FinanzasRepository>();
        services.AddScoped<ITareaRepository, TareaRepository>();
        services.AddScoped<INotificacionesRepository, NotificacionesRepository>();
        services.AddScoped<IPushNotificationService, PushNotificationService>();

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
