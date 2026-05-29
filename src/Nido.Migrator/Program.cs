using Microsoft.EntityFrameworkCore;
using Nido.Infrastructure.Persistence;
using Nido.Application.Electrodomesticos;

AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);

DotNetEnv.Env.TraversePath().Load();

var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection")
    ?? throw new InvalidOperationException(
        "Missing required environment variable: ConnectionStrings__DefaultConnection. " +
        "Set it directly or in a .env file.");

Console.WriteLine("Nido.Migrator — starting database migration");

var options = new DbContextOptionsBuilder<NidoDbContext>()
    .UseNpgsql(connectionString)
    .Options;

try
{
    await using var db = new NidoDbContext(options);

    var pending = (await db.Database.GetPendingMigrationsAsync()).ToList();

    if (pending.Count == 0)
    {
        Console.WriteLine("No pending migrations. Database is up to date.");
        return 0;
    }

    Console.WriteLine($"Applying {pending.Count} pending migration(s):");
    foreach (var migration in pending)
    {
        Console.WriteLine($"  - {migration}");
    }

    await db.Database.MigrateAsync();

    Console.WriteLine("All migrations applied successfully.");
    return 0;
}
catch (Exception ex)
{
    Console.Error.WriteLine($"Migration failed: {ex.Message}");
    Console.Error.WriteLine(ex.ToString());
    return 1;
}
