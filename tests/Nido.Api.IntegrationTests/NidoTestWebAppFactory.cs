using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Nido.Infrastructure.Persistence;

namespace Nido.Api.IntegrationTests;

/// <summary>
/// Replaces the real PostgreSQL DbContext with an SQLite in-memory database
/// so integration tests run in CI without a database server.
/// </summary>
public sealed class NidoTestWebAppFactory : WebApplicationFactory<Program>
{
    private readonly SqliteConnection _connection = new("DataSource=:memory:");

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // Provide a dummy connection string so DependencyInjection.cs doesn't throw
        // before ConfigureTestServices has a chance to replace the DbContext.
        builder.UseSetting(
            "ConnectionStrings:DefaultConnection",
            "Host=localhost;Database=nido_ci;Username=ci;Password=ci");

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
        });
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing) _connection.Dispose();
        base.Dispose(disposing);
    }
}
