using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Nido.Infrastructure.Persistence;

public sealed class NidoDesignTimeDbContextFactory : IDesignTimeDbContextFactory<NidoDbContext>
{
    public NidoDbContext CreateDbContext(string[] args)
    {
        var connectionString = ResolveConnectionString();

        var optionsBuilder = new DbContextOptionsBuilder<NidoDbContext>();
        optionsBuilder.UseNpgsql(connectionString);

        return new NidoDbContext(optionsBuilder.Options);
    }

    private static string ResolveConnectionString()
    {
        var fromEnvironment = Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection")
            ?? Environment.GetEnvironmentVariable("ConnectionStrings__Nido");
        if (!string.IsNullOrWhiteSpace(fromEnvironment))
        {
            return fromEnvironment;
        }

        var envPath = FindDotEnvPath();
        if (envPath is not null)
        {
            var fromDotEnv = ReadValueFromDotEnv(envPath, "ConnectionStrings__DefaultConnection")
                ?? ReadValueFromDotEnv(envPath, "ConnectionStrings__Nido");
            if (!string.IsNullOrWhiteSpace(fromDotEnv))
            {
                return fromDotEnv;
            }
        }

        throw new InvalidOperationException(
            "Could not resolve database connection for EF Core design-time tooling. " +
            "Set ConnectionStrings__DefaultConnection (or legacy ConnectionStrings__Nido) as an environment variable or in repository .env.");
    }

    private static string? FindDotEnvPath()
    {
        var current = new DirectoryInfo(Directory.GetCurrentDirectory());
        while (current is not null)
        {
            var candidate = Path.Combine(current.FullName, ".env");
            if (File.Exists(candidate))
            {
                return candidate;
            }

            current = current.Parent;
        }

        return null;
    }

    private static string? ReadValueFromDotEnv(string path, string keyToFind)
    {
        foreach (var rawLine in File.ReadLines(path))
        {
            var line = rawLine.Trim();
            if (line.Length == 0 || line.StartsWith('#'))
            {
                continue;
            }

            var separator = line.IndexOf('=');
            if (separator <= 0)
            {
                continue;
            }

            var key = line[..separator].Trim();
            var value = line[(separator + 1)..].Trim();

            if ((value.StartsWith('"') && value.EndsWith('"')) || (value.StartsWith('\'') && value.EndsWith('\'')))
            {
                value = value[1..^1];
            }

            if (string.Equals(key, keyToFind, StringComparison.OrdinalIgnoreCase))
            {
                return value;
            }
        }

        return null;
    }
}
