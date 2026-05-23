using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Nido.Infrastructure.Persistence;

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

        return services;
    }
}
