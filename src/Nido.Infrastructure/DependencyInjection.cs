using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Nido.Application.Households;
using Nido.Infrastructure.Households;
using Nido.Infrastructure.Persistence;

namespace Nido.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddNidoInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Nido");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException("Missing required configuration: ConnectionStrings:Nido");
        }

        var serverVersion = new MySqlServerVersion(new Version(8, 0, 36));

        services.AddDbContext<NidoDbContext>(options =>
            options.UseMySql(connectionString, serverVersion));

        services.AddScoped<IHouseholdRepository, EfHouseholdRepository>();

        return services;
    }
}
