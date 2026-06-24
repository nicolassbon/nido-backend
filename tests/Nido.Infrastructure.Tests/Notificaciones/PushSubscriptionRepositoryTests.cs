using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Nido.Infrastructure.Notificaciones;
using Nido.Infrastructure.Persistence;
using Nido.Infrastructure.Persistence.Entities;
using Nido.Tests.Shared;
using Xunit;

namespace Nido.Infrastructure.Tests.Notificaciones;

public sealed class PushSubscriptionRepositoryTests : IAsyncLifetime
{
    private readonly PostgresTestServer _server = PostgresTestServer.GetSharedAsync().GetAwaiter().GetResult();
    private PostgresTestDatabase _database = null!;
    private NidoDbContext _db = null!;

    public async Task InitializeAsync()
    {
        _database = await _server.CreateDatabaseAsync("push_subscription_repository_tests");

        var options = new DbContextOptionsBuilder<NidoDbContext>()
            .UseNpgsql(_database.ConnectionString)
            .ConfigureWarnings(w => w.Ignore(RelationalEventId.PendingModelChangesWarning))
            .Options;

        _db = new NidoDbContext(options);
        await _db.Database.MigrateAsync();
    }

    public async Task DisposeAsync()
    {
        await _db.DisposeAsync();
        await _database.DisposeAsync();
    }

    [Fact]
    public async Task SubscribePushAsync_WhenCalledTwiceForSameEndpoint_UpdatesExistingRow()
    {
        var usuarioId = await SeedUserAsync();
        var sut = new NotificacionesRepository(_db);

        await sut.SubscribePushAsync(usuarioId, "https://push.test/subscriptions/1", "key-a", "auth-a", CancellationToken.None);
        await sut.SubscribePushAsync(usuarioId, "https://push.test/subscriptions/1", "key-b", "auth-b", CancellationToken.None);

        var subscriptions = await _db.SuscripcionesPush
            .AsNoTracking()
            .Where(x => x.UsuarioId == usuarioId)
            .ToListAsync();

        var subscription = Assert.Single(subscriptions);
        Assert.Equal("key-b", subscription.P256dh);
        Assert.Equal("auth-b", subscription.Auth);
    }

    [Fact]
    public async Task SubscribePushAsync_WhenConcurrentRequestsUseSameEndpoint_PersistsSingleRow()
    {
        var usuarioId = await SeedUserAsync();
        await using var db1 = CreateDbContext();
        await using var db2 = CreateDbContext();
        var repo1 = new NotificacionesRepository(db1);
        var repo2 = new NotificacionesRepository(db2);
        var start = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        async Task SubscribeAsync(NotificacionesRepository repository)
        {
            await start.Task;
            await repository.SubscribePushAsync(usuarioId, "https://push.test/subscriptions/race", "key-a", "auth-a", CancellationToken.None);
        }

        var firstTask = Task.Run(() => SubscribeAsync(repo1));
        var secondTask = Task.Run(() => SubscribeAsync(repo2));
        start.SetResult();

        await Task.WhenAll(firstTask, secondTask);

        var subscriptions = await _db.SuscripcionesPush
            .AsNoTracking()
            .Where(x => x.UsuarioId == usuarioId && x.Endpoint == "https://push.test/subscriptions/race")
            .ToListAsync();

        Assert.Single(subscriptions);
    }

    private async Task<Guid> SeedUserAsync()
    {
        var usuarioId = Guid.NewGuid();
        _db.Usuarios.Add(new Usuario
        {
            Id = usuarioId,
            Nombre = "Push User",
            Email = $"push-{Guid.NewGuid():N}@test.local",
            Sexo = "U",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        });
        await _db.SaveChangesAsync();
        _db.ChangeTracker.Clear();
        return usuarioId;
    }

    private NidoDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<NidoDbContext>()
            .UseNpgsql(_database.ConnectionString)
            .ConfigureWarnings(w => w.Ignore(RelationalEventId.PendingModelChangesWarning))
            .Options;

        return new NidoDbContext(options);
    }
}
