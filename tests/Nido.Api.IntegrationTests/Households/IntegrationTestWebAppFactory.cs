using System;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Nido.Infrastructure.Persistence;

namespace Nido.Api.IntegrationTests.Households;

public sealed class IntegrationTestWebAppFactory : WebApplicationFactory<Program>, IDisposable
{
    private readonly SqliteConnection _connection = new("DataSource=:memory:");

    public IntegrationTestWebAppFactory()
    {
        if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("ConnectionStrings__Nido")))
        {
            Environment.SetEnvironmentVariable("ConnectionStrings__Nido", "DummyConnectionStringForTests");
        }
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        _connection.Open();

        builder.ConfigureAppConfiguration((context, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                { "ConnectionStrings:Nido", "DummyConnectionStringForTests" }
            });
        });

        builder.ConfigureServices(services =>
        {
            services.RemoveAll(typeof(DbContextOptions<NidoDbContext>));
            services.RemoveAll(typeof(IDbContextOptionsConfiguration<NidoDbContext>));

            services.AddDbContext<NidoDbContext>(options => options.UseSqlite(_connection));

            using var scope = services.BuildServiceProvider().CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<NidoDbContext>();
            dbContext.Database.EnsureCreated();
        });
    }

    public void ResetDatabase()
    {
        using var scope = Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<NidoDbContext>();
        dbContext.Database.EnsureDeleted();
        dbContext.Database.EnsureCreated();
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing)
        {
            _connection.Dispose();
        }
    }
}
