using Microsoft.EntityFrameworkCore;
using Nido.Infrastructure.Persistence;
using Npgsql;

namespace Nido.Tests.Shared;

public sealed class PostgresTestDatabase : IAsyncDisposable
{
    private readonly string _adminConnectionString;

    public PostgresTestDatabase(string adminConnectionString, string tag)
    {
        _adminConnectionString = adminConnectionString;
        DatabaseName = $"nido_test_{Sanitize(tag)}_{Guid.NewGuid():N}";

        var builder = new NpgsqlConnectionStringBuilder(adminConnectionString)
        {
            Database = DatabaseName,
            Pooling = false
        };

        ConnectionString = builder.ConnectionString;
    }

    public string DatabaseName { get; }

    public string ConnectionString { get; }

    public async Task CreateAsync(CancellationToken cancellationToken = default)
    {
        await using var connection = new NpgsqlConnection(_adminConnectionString);
        await connection.OpenAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = $"CREATE DATABASE \"{DatabaseName}\"";
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public DbContextOptions<NidoDbContext> CreateDbContextOptions()
        => new DbContextOptionsBuilder<NidoDbContext>()
            .UseNpgsql(ConnectionString)
            .Options;

    public async Task MigrateAsync(CancellationToken cancellationToken = default)
    {
        await using var dbContext = new NidoDbContext(CreateDbContextOptions());
        await dbContext.Database.MigrateAsync(cancellationToken);
    }

    public async ValueTask DisposeAsync()
    {
        NpgsqlConnection.ClearAllPools();

        await using var connection = new NpgsqlConnection(_adminConnectionString);
        await connection.OpenAsync();
        await using (var terminateConnections = connection.CreateCommand())
        {
            terminateConnections.CommandText = $"""
                SELECT pg_terminate_backend(pid)
                FROM pg_stat_activity
                WHERE datname = '{DatabaseName}'
                  AND pid <> pg_backend_pid();
                """;
            await terminateConnections.ExecuteNonQueryAsync();
        }

        await using var dropDatabase = connection.CreateCommand();
        dropDatabase.CommandText = $"DROP DATABASE IF EXISTS \"{DatabaseName}\";";
        await dropDatabase.ExecuteNonQueryAsync();
    }

    private static string Sanitize(string value)
    {
        var sanitized = new string(value
            .ToLowerInvariant()
            .Select(ch => char.IsLetterOrDigit(ch) ? ch : '_')
            .ToArray())
            .Trim('_');

        return string.IsNullOrWhiteSpace(sanitized) ? "db" : sanitized;
    }
}
