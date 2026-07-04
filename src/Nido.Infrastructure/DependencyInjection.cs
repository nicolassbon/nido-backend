using Amazon.Runtime;
using Amazon.S3;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
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
using Nido.Application.Common.Security;
using Nido.Application.Common.Storage;
using Nido.Application.Electrodomesticos.UploadElectrodomesticoImage;
using Nido.Application.Productos;
using Nido.Application.Preferencias;
using Nido.Application.Recetas;
using Nido.Application.Estadisticas;
using Nido.Application.ListaCompras;
using Nido.Infrastructure.Estadisticas;
using Nido.Infrastructure.Alacena;
using Nido.Infrastructure.ListaCompras;
using Nido.Infrastructure.Productos;
using Nido.Application.UsuariosPerfil;
using Nido.Application.Telegram.Messaging;
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
using Nido.Application.Telegram.Client;
using Nido.Application.Telegram.Idempotency;
using Nido.Application.Telegram.Authorization;
using Nido.Application.Telegram.Conversation;
using Nido.Application.Telegram.Menu;
using Nido.Application.Telegram.Pairing;
using Nido.Application.Telegram.Webhook;
using Nido.Infrastructure.Notificaciones;
using Nido.Application.Planificador;
using Nido.Infrastructure.Planificador;
using Nido.Infrastructure.Telegram.Idempotency;
using Nido.Infrastructure.Telegram.Authorization;
using Nido.Infrastructure.Telegram.Conversation;
using Nido.Infrastructure.Telegram.Menu;
using Nido.Infrastructure.Telegram.Pairing;
using Nido.Infrastructure.Telegram;
using Nido.Infrastructure.Telegram.Webhook;
using Nido.Infrastructure.Telegram.Messaging;
using Resend;
using Nido.Application.Tickets;
using Nido.Infrastructure.Tickets;

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
        services.AddScoped<IHogarMembershipRepository, HogarMembershipRepository>();
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
        services.AddScoped<INutritionInfoRepository, ProductNutritionRepository>();
        services.AddScoped<IListaComprasRepository, ListaComprasRepository>();
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
        //google document ai
        services.AddOptions<GoogleDocumentAiOptions>()
    .Bind(configuration.GetSection(GoogleDocumentAiOptions.SectionName));
        services.AddScoped<IFinanzasRepository, FinanzasRepository>();
        services.AddScoped<ITareaRepository, TareaRepository>();
        services.AddScoped<INotificacionesRepository, NotificacionesRepository>();
        services.AddScoped<IPushNotificationService, PushNotificationService>();

        services.AddHttpClient("TelegramClient", (sp, client) =>
        {
            var opts = sp.GetRequiredService<IOptions<Application.Telegram.TelegramOptions>>().Value;

            if (opts.HasBotToken)
            {
                client.BaseAddress = new Uri($"https://api.telegram.org/bot{opts.BotToken}/");
            }

            client.Timeout = TimeSpan.FromSeconds(opts.TimeoutSeconds);
        });
        services.AddScoped<ITelegramClient>(sp =>
        {
            var opts = sp.GetRequiredService<IOptions<Application.Telegram.TelegramOptions>>().Value;
            if (!opts.HasBotToken)
            {
                return new DisabledTelegramClient(sp.GetRequiredService<ILogger<DisabledTelegramClient>>());
            }

            var httpClient = sp.GetRequiredService<IHttpClientFactory>().CreateClient("TelegramClient");
            return new Telegram.TelegramClient(httpClient, sp.GetRequiredService<ILogger<Telegram.TelegramClient>>());
        });

        services.AddSingleton<ITelegramWebhookTelemetry, TelegramWebhookTelemetry>();
        services.AddSingleton<ITelegramOutboxWakeupService, TelegramOutboxWakeupService>();
        services.AddScoped<ITelegramUpdateIdempotencyService, TelegramUpdateIdempotencyService>();
        services.AddScoped<ITelegramWebhookHandler, TelegramWebhookHandler>();
        services.AddScoped<ITelegramHogarAccess, TelegramHogarAccessRepository>();
        services.AddScoped<ITelegramPairingRepository, TelegramPairingRepository>();
        services.AddScoped<ITelegramPairingTokenHasher, TelegramPairingTokenHasher>();
        services.AddScoped<ITelegramPairingRateLimiter, TelegramPairingRateLimiter>();
        services.AddScoped<ITelegramConversationStateStore, PostgresTelegramConversationStateStore>();
        services.AddScoped<ITelegramOutboxWriter, TelegramOutboxWriter>();
        services.AddScoped<ITelegramOutboxReader, TelegramOutboxReader>();
        services.AddScoped<ITelegramMenuRegistry, InMemoryTelegramMenuRegistry>();
        services.AddScoped<ITelegramMenuReadService, EfTelegramMenuReadService>();
        services.AddScoped<ITelegramMenuProvider, TelegramMenuProvider>();
        services.AddScoped<ITelegramNotificationBatcher, TelegramNotificationBatcher>();

        // ── Lookup externo de productos por barcode ───────────────────────────────────────────
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

        services.AddScoped<CatalogoRepository>();
        services.AddScoped<IPlanificadorRepository, PlanificadorRepository>();
        services.AddScoped<PlanificadorHandler>();

        services.AddScoped<IReceiptParser, GoogleDocumentAiReceiptParser>();
        services.AddScoped<INutritionLabelParser, GoogleDocumentAiNutritionLabelParser>();

        services.AddHostedService<AlertaDiariaWorker>();

        // Price Comparator
        services.AddHttpClient<IPriceComparatorService, PriceComparatorService>(client =>
        {
            client.BaseAddress = new Uri(configuration["ExternalServices:ComparatorUrl"] ?? "http://nido-comparator:3000/");
            client.Timeout = TimeSpan.FromSeconds(15);
        });
        services.AddScoped<ComparePricesHandler>();

        return services;
    }

    public static IServiceCollection AddTelegramSenderWorker(this IServiceCollection services)
    {
        throw new NotSupportedException("Use the AddTelegramSenderWorker(IServiceCollection, IConfiguration) overload.");
    }

    public static IServiceCollection AddTelegramSenderWorker(this IServiceCollection services, IConfiguration configuration)
    {
        var options = configuration.GetSection(Application.Telegram.TelegramOptions.SectionName)
            .Get<Application.Telegram.TelegramOptions>()
            ?? new Application.Telegram.TelegramOptions();

        if (options.HasBotToken)
        {
            services.AddHostedService(sp => (TelegramOutboxWakeupService)sp.GetRequiredService<ITelegramOutboxWakeupService>());
            services.AddHostedService<TelegramBatchingWorker>();
            services.AddHostedService<TelegramSenderWorker>();
        }

        return services;
    }
}
