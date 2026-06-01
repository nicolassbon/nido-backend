using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Nido.Application.Auth;
using Nido.Application.Auth.Helpers;
using Nido.Application.Auth.Interfaces;
using Nido.Application.Auth.RefreshToken;
using Nido.Application.Common.ProfileImages;
using Nido.Infrastructure.Persistence;

namespace Nido.Api.IntegrationTests;

/// <summary>
/// Replaces the real PostgreSQL DbContext with an SQLite in-memory database
/// so integration tests run in CI without a database server.
/// </summary>
public sealed class NidoTestWebAppFactory : WebApplicationFactory<Program>
{
    private readonly SqliteConnection _connection = new("DataSource=:memory:");
    private readonly Action<IServiceCollection>? _configureStorage;
    private readonly Action<IApplicationBuilder>? _configureAfterApp;

    public NidoTestWebAppFactory()
    {
    }

    private NidoTestWebAppFactory(Action<IServiceCollection>? configureStorage, Action<IApplicationBuilder>? configureAfterApp = null)
    {
        _configureStorage = configureStorage;
        _configureAfterApp = configureAfterApp;
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        // Provide a dummy connection string so DependencyInjection.cs doesn't throw
        // before ConfigureTestServices has a chance to replace the DbContext.
        builder.UseSetting(
            "ConnectionStrings:DefaultConnection",
            "Host=localhost;Database=nido_ci;Username=ci;Password=ci");

        // Provide a JWT key so JwtTokenService doesn't throw in tests.
        // This key has no security value — it is only used in the in-memory test environment.
        builder.UseSetting("Jwt:Key", "integration-test-jwt-key-minimum-32-bytes-long!!");
        builder.UseSetting("Jwt:Issuer", "nido-api-tests");
        builder.UseSetting("Jwt:Audience", "nido-clients-tests");
        builder.UseSetting("Google:ClientId", "test-google-client-id.apps.googleusercontent.com");

        // Override any .env / environment-variable values that would otherwise leak into tests.
        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
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

        // ConfigureTestServices runs AFTER the app's own ConfigureServices,
        // so we can cleanly replace the Npgsql registration with SQLite.
        builder.ConfigureTestServices(services =>
        {
            // Remove DbContextOptions<NidoDbContext> (the computed options object)
            var optionsDescriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(DbContextOptions<NidoDbContext>));
            if (optionsDescriptor is not null)
                services.Remove(optionsDescriptor);

            // Remove IDbContextOptionsConfiguration<NidoDbContext> (the Npgsql lambda)
            // These are the per-context configuration actions that EF Core merges.
            var configType = typeof(IDbContextOptionsConfiguration<NidoDbContext>);
            foreach (var d in services.Where(d => d.ServiceType == configType).ToList())
                services.Remove(d);

            // Keep one open connection so the SQLite in-memory DB persists
            // across all requests within this factory's lifetime.
            _connection.CreateFunction<string>("now", () => DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss"));
            _connection.CreateFunction<string>("uuid_generate_v4", () => Guid.NewGuid().ToString());
            _connection.Open();

            services.AddDbContext<NidoDbContext>(options =>
                options.UseSqlite(_connection)
                       .ConfigureWarnings(w => w.Ignore(RelationalEventId.PendingModelChangesWarning)));
            // Schema is created by MigrateAsync() in Program.cs startup.

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
        => new(configureStorage, _configureAfterApp);

    public NidoTestWebAppFactory WithAfterAppConfiguration(Action<IApplicationBuilder> configureAfterApp)
        => new(_configureStorage, configureAfterApp);

    protected override void Dispose(bool disposing)
    {
        if (disposing) _connection.Dispose();
        base.Dispose(disposing);
    }

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
