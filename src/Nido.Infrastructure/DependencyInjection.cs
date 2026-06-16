using Amazon.Runtime;
using Amazon.S3;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
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
using Nido.Infrastructure.Storage;
using Nido.Application.Productos.UploadProductImage;
using Nido.Application.Recetas.UploadRecipeImage;
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

        services.AddScoped<IReceiptParser, GoogleDocumentAiReceiptParser>();
        return services;
    }
}
