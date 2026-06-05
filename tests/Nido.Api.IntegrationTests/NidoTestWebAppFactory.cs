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
using Nido.Application.Common.ProfileImages;
using Nido.Infrastructure.Persistence;
using Nido.Tests.Shared;

namespace Nido.Api.IntegrationTests;

/// <summary>
/// Replaces the app's production PostgreSQL registration with an isolated PostgreSQL test database.
/// </summary>
public sealed class NidoTestWebAppFactory : WebApplicationFactory<Program>
{
    private readonly Action<IServiceCollection>? _configureStorage;
    private readonly Action<IApplicationBuilder>? _configureAfterApp;
    private readonly PostgresTestDatabase _testDatabase;
    private readonly bool _ownsTestDatabase;

    public NidoTestWebAppFactory()
        : this(
            configureStorage: null,
            configureAfterApp: null,
            testDatabase: CreateDatabase("api_factory"),
            ownsTestDatabase: true)
    {
    }

    private NidoTestWebAppFactory(
        Action<IServiceCollection>? configureStorage,
        Action<IApplicationBuilder>? configureAfterApp,
        PostgresTestDatabase testDatabase,
        bool ownsTestDatabase)
    {
        _configureStorage = configureStorage;
        _configureAfterApp = configureAfterApp;
        _testDatabase = testDatabase;
        _ownsTestDatabase = ownsTestDatabase;
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.UseSetting("ConnectionStrings:DefaultConnection", _testDatabase.ConnectionString);

        builder.UseSetting("Jwt:Key", "integration-test-jwt-key-minimum-32-bytes-long!!");
        builder.UseSetting("Jwt:Issuer", "nido-api-tests");
        builder.UseSetting("Jwt:Audience", "nido-clients-tests");
        builder.UseSetting("Google:ClientId", "test-google-client-id.apps.googleusercontent.com");

        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
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
                ["ProfileImages:PublicBaseUrl"] = "https://cdn.test.local"
            });
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

            _configureStorage?.Invoke(services);

            if (_configureAfterApp is not null)
            {
                services.AddTransient<IStartupFilter>(_ => new AfterAppStartupFilter(_configureAfterApp));
            }
        });
    }

    public static void ReplaceProfileImageStorage(IServiceCollection services, IProfileImageStorage storage)
    {
        services.RemoveAll<IProfileImageStorage>();
        services.AddSingleton(storage);
    }

    public NidoTestWebAppFactory WithStorageOverride(Action<IServiceCollection> configureStorage)
        => new(configureStorage, _configureAfterApp, _testDatabase, ownsTestDatabase: false);

    public NidoTestWebAppFactory WithAfterAppConfiguration(Action<IApplicationBuilder> configureAfterApp)
        => new(_configureStorage, configureAfterApp, _testDatabase, ownsTestDatabase: false);

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
