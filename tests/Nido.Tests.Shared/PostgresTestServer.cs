using Testcontainers.PostgreSql;

namespace Nido.Tests.Shared;

public sealed class PostgresTestServer : IAsyncDisposable
{
    private static readonly Lazy<Task<PostgresTestServer>> SharedInstance = new(CreateSharedAsync);

    static PostgresTestServer()
    {
        AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);
    }

    private readonly PostgreSqlContainer _container;

    private PostgresTestServer(PostgreSqlContainer container)
    {
        _container = container;
        AdminConnectionString = container.GetConnectionString();
    }

    public string AdminConnectionString { get; }

    public static Task<PostgresTestServer> GetSharedAsync() => SharedInstance.Value;

    public async Task<PostgresTestDatabase> CreateDatabaseAsync(string tag, CancellationToken cancellationToken = default)
    {
        var database = new PostgresTestDatabase(AdminConnectionString, tag);
        await database.CreateAsync(cancellationToken);
        return database;
    }

    public async ValueTask DisposeAsync()
    {
        await _container.DisposeAsync();
    }

    private static async Task<PostgresTestServer> CreateSharedAsync()
    {
        var container = new PostgreSqlBuilder()
            .WithImage("postgres:17")
            .Build();

        await container.StartAsync();
        return new PostgresTestServer(container);
    }
}
