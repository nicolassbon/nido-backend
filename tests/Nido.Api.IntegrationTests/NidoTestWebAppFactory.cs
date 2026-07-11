using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Nido.Application.Common.ProfileImages;
using Nido.Infrastructure.Persistence;
using Nido.Tests.Shared;
using Nido.Application.Common.Storage;

namespace Nido.Api.IntegrationTests;

public sealed class NidoTestWebAppFactory : WebApplicationFactory<Program>
{
    private readonly Action<IServiceCollection>? _configureStorage;
    private readonly Action<IApplicationBuilder>? _configureAfterApp;
    private readonly IReadOnlyDictionary<string, string?>? _extraConfiguration;
    private readonly string _environment;
    private readonly TestLogCapture? _logCapture;
    private readonly PostgresTestDatabase _testDatabase;
    private readonly bool _ownsTestDatabase;

    public NidoTestWebAppFactory()
        : this(
            configureStorage: null,
            configureAfterApp: null,
            extraConfiguration: null,
            environment: "Testing",
            logCapture: null,
            testDatabase: CreateDatabase("api_factory"),
            ownsTestDatabase: true)
    {
    }

    private NidoTestWebAppFactory(
        Action<IServiceCollection>? configureStorage,
        Action<IApplicationBuilder>? configureAfterApp,
        IReadOnlyDictionary<string, string?>? extraConfiguration,
        string environment,
        TestLogCapture? logCapture,
        PostgresTestDatabase testDatabase,
        bool ownsTestDatabase)
    {
        _configureStorage = configureStorage;
        _configureAfterApp = configureAfterApp;
        _extraConfiguration = extraConfiguration;
        _environment = environment;
        _logCapture = logCapture;
        _testDatabase = testDatabase;
        _ownsTestDatabase = ownsTestDatabase;
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment(_environment);
        builder.UseSetting("hostBuilder:reloadConfigOnChange", "false");
        builder.UseSetting("ConnectionStrings:DefaultConnection", _testDatabase.ConnectionString);

        builder.UseSetting("Jwt:Key", "integration-test-jwt-key-minimum-32-bytes-long!!");
        builder.UseSetting("Jwt:Issuer", "nido-api-tests");
        builder.UseSetting("Jwt:Audience", "nido-clients-tests");
        builder.UseSetting("Google:ClientId", "test-google-client-id.apps.googleusercontent.com");

        builder.UseSetting("Telegram:BotToken", "default-test-bot-token");
        builder.UseSetting("Telegram:WebhookSecretToken", "default-test-webhook-secret");
        builder.UseSetting("MercadoPago:AccessToken", "TEST-mercadopago-access-token");
        builder.UseSetting("MercadoPago:Mode", "Sandbox");
        builder.UseSetting("MercadoPago:WebhookSecret", "test-mercadopago-webhook-secret");
        builder.UseSetting("MercadoPago:PublicKey", "TEST-mercadopago-public-key");
        builder.UseSetting("MercadoPago:ApiBaseUrl", "https://api.mercadopago.test");

        if (_extraConfiguration is not null)
        {
            foreach (var (key, value) in _extraConfiguration)
            {
                builder.UseSetting(key, value);
            }
        }

        builder.ConfigureAppConfiguration((_, config) =>
        {
            var values = new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] = _testDatabase.ConnectionString,
                ["Jwt:Key"] = "integration-test-jwt-key-minimum-32-bytes-long!!",
                ["Jwt:Issuer"] = "nido-api-tests",
                ["Jwt:Audience"] = "nido-clients-tests",
                ["Google:ClientId"] = "test-google-client-id.apps.googleusercontent.com",
                ["Frontend:BaseUrl"] = "http://localhost:4200",
                ["PasswordReset:TokenExpiryMinutes"] = "60",
                ["ProfileImages:MaxBytes"] = "5242880",
                ["ProfileImages:MaxDimension"] = "512",
                ["ProfileImages:WebpQuality"] = "80",
                ["ProfileImages:PublicBaseUrl"] = "https://cdn.test.local",
                ["MercadoPago:AccessToken"] = "TEST-mercadopago-access-token",
                ["MercadoPago:Mode"] = "Sandbox",
                ["MercadoPago:WebhookSecret"] = "test-mercadopago-webhook-secret",
                ["MercadoPago:PublicKey"] = "TEST-mercadopago-public-key",
                ["MercadoPago:ApiBaseUrl"] = "https://api.mercadopago.test"
            };

            if (_extraConfiguration is not null)
            {
                foreach (var (key, value) in _extraConfiguration)
                {
                    values[key] = value;
                }
            }

            config.AddInMemoryCollection(values);
        });

        builder.ConfigureTestServices(services =>
        {
            var optionsDescriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(DbContextOptions<NidoDbContext>));
            if (optionsDescriptor is not null)
            {
                services.Remove(optionsDescriptor);
            }

            var configType = typeof(IDbContextOptionsConfiguration<NidoDbContext>);
            foreach (var descriptor in services.Where(d => d.ServiceType == configType).ToList())
            {
                services.Remove(descriptor);
            }

            services.AddDbContext<NidoDbContext>(options =>
                options.UseNpgsql(_testDatabase.ConnectionString)
                    .ConfigureWarnings(w => w.Ignore(RelationalEventId.PendingModelChangesWarning)));

            if (_logCapture is not null)
            {
                services.AddLogging(logging => logging.AddProvider(_logCapture));
            }

            _configureStorage?.Invoke(services);

            if (_configureAfterApp is not null)
            {
                services.AddTransient<IStartupFilter>(_ => new AfterAppStartupFilter(_configureAfterApp));
            }
        });
    }

    public static void ReplaceFileStorageService(IServiceCollection services, IFileStorageService storage)
    {
        services.RemoveAll<IFileStorageService>();
        services.AddSingleton(storage);
    }


    public NidoTestWebAppFactory WithStorageOverride(Action<IServiceCollection> configureStorage)
        => new(configureStorage, _configureAfterApp, _extraConfiguration, _environment, _logCapture, _testDatabase, ownsTestDatabase: false);

    public NidoTestWebAppFactory WithAfterAppConfiguration(Action<IApplicationBuilder> configureAfterApp)
        => new(_configureStorage, configureAfterApp, _extraConfiguration, _environment, _logCapture, _testDatabase, ownsTestDatabase: false);

    public NidoTestWebAppFactory WithConfiguration(IReadOnlyDictionary<string, string?> configuration)
    {
        var merged = new Dictionary<string, string?>(_extraConfiguration ?? new Dictionary<string, string?>());
        foreach (var (key, value) in configuration)
        {
            merged[key] = value;
        }

        return new NidoTestWebAppFactory(
            configureStorage: _configureStorage,
            configureAfterApp: _configureAfterApp,
            extraConfiguration: merged,
            environment: _environment,
            logCapture: _logCapture,
            testDatabase: _testDatabase,
            ownsTestDatabase: false);
    }

    public NidoTestWebAppFactory WithTelegramWebhookConfig(
        string secret,
        int? maxPayloadBytes = null,
        int? rateLimitPermitPerWindow = null,
        int? rateLimitWindowSeconds = null)
    {
        var merged = new Dictionary<string, string?>(_extraConfiguration ?? new Dictionary<string, string?>())
        {
            ["Telegram:WebhookSecretToken"] = secret
        };
        if (maxPayloadBytes.HasValue) merged["Telegram:WebhookMaxPayloadBytes"] = maxPayloadBytes.Value.ToString();
        if (rateLimitPermitPerWindow.HasValue) merged["Telegram:WebhookRateLimitPermitPerWindow"] = rateLimitPermitPerWindow.Value.ToString();
        if (rateLimitWindowSeconds.HasValue) merged["Telegram:WebhookRateLimitWindowSeconds"] = rateLimitWindowSeconds.Value.ToString();

        return new NidoTestWebAppFactory(
            configureStorage: _configureStorage,
            configureAfterApp: _configureAfterApp,
            extraConfiguration: merged,
            environment: _environment,
            logCapture: _logCapture,
            testDatabase: _testDatabase,
            ownsTestDatabase: false);
    }

    public NidoTestWebAppFactory WithLogCapture(TestLogCapture capture)
        => new(_configureStorage, _configureAfterApp, _extraConfiguration, _environment, capture, _testDatabase, ownsTestDatabase: false);

    public NidoTestWebAppFactory WithEnvironment(string environment)
        => new(_configureStorage, _configureAfterApp, _extraConfiguration, environment, _logCapture, _testDatabase, ownsTestDatabase: false);

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

        if (disposing && _ownsTestDatabase)
        {
            _testDatabase.DisposeAsync().AsTask().GetAwaiter().GetResult();
        }
    }

    private static PostgresTestDatabase CreateDatabase(string tag)
        => PostgresTestServer
            .GetSharedAsync()
            .GetAwaiter()
            .GetResult()
            .CreateDatabaseAsync(tag)
            .GetAwaiter()
            .GetResult();

    private sealed class AfterAppStartupFilter : IStartupFilter
    {
        private readonly Action<IApplicationBuilder> _configure;

        public AfterAppStartupFilter(Action<IApplicationBuilder> configure)
        {
            _configure = configure;
        }

        public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next)
        {
            return app =>
            {
                next(app);
                _configure(app);
            };
        }
    }
}
