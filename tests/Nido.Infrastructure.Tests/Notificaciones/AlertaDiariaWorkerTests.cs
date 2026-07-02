using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Nido.Application.Notificaciones;
using Nido.Infrastructure.Notificaciones;
using Nido.Infrastructure.Persistence;
using Nido.Infrastructure.Persistence.Entities;
using Nido.Tests.Shared;
using Xunit;

namespace Nido.Infrastructure.Tests.Notificaciones;

public sealed class AlertaDiariaWorkerTests : IAsyncLifetime
{
    private readonly PostgresTestServer _server = PostgresTestServer.GetSharedAsync().GetAwaiter().GetResult();
    private PostgresTestDatabase _database = null!;
    private NidoDbContext _db = null!;
    private IServiceProvider _serviceProvider = null!;

    public async Task InitializeAsync()
    {
        _database = await _server.CreateDatabaseAsync("alerta_diaria_worker_tests");

        var options = new DbContextOptionsBuilder<NidoDbContext>()
            .UseNpgsql(_database.ConnectionString)
            .ConfigureWarnings(w => w.Ignore(RelationalEventId.PendingModelChangesWarning))
            .Options;

        _db = new NidoDbContext(options);
        await _db.Database.MigrateAsync();

        // Set up service provider for the background scope resolution
        var services = new ServiceCollection();
        services.AddSingleton(options);
        services.AddScoped(sp => new NidoDbContext(sp.GetRequiredService<DbContextOptions<NidoDbContext>>()));
        
        var mockRepo = new MockNotificacionesRepository();
        services.AddSingleton<INotificacionesRepository>(mockRepo);

        _serviceProvider = services.BuildServiceProvider();
    }

    public async Task DisposeAsync()
    {
        await _db.DisposeAsync();
        await _database.DisposeAsync();
    }

    [Fact]
    public async Task Worker_ProcessesAllUsers_WhenExecuted()
    {
        // Arrange
        var user1Id = Guid.NewGuid();
        var user2Id = Guid.NewGuid();
        _db.Usuarios.AddRange(
            new Usuario { Id = user1Id, Nombre = "User 1", Email = "u1@test.local", Sexo = "M", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow },
            new Usuario { Id = user2Id, Nombre = "User 2", Email = "u2@test.local", Sexo = "F", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow }
        );
        await _db.SaveChangesAsync();

        var repo = (MockNotificacionesRepository)_serviceProvider.GetRequiredService<INotificacionesRepository>();
        
        // 10:59:59 UTC means 11:00:00 UTC execution is in 1 second
        var fakeTime = new FakeTimeProvider(new DateTimeOffset(2026, 6, 28, 10, 59, 59, TimeSpan.Zero));
        var sut = new AlertaDiariaWorker(_serviceProvider, NullLogger<AlertaDiariaWorker>.Instance, fakeTime);

        // Act
        using var cts = new CancellationTokenSource();
        var runTask = sut.StartAsync(cts.Token);

        // Wait a small moment to let the target execution complete
        await Task.Delay(1500);

        // Cancel the worker to clean up
        await cts.CancelAsync();
        
        try
        {
            await runTask;
        }
        catch (OperationCanceledException)
        {
            // Expected
        }

        // Assert
        Assert.Contains(user1Id, repo.CalledUserIds);
        Assert.Contains(user2Id, repo.CalledUserIds);
    }

    private sealed class MockNotificacionesRepository : INotificacionesRepository
    {
        public System.Collections.Concurrent.ConcurrentBag<Guid> CalledUserIds { get; } = new();

        public Task<System.Collections.Generic.List<NotificacionResult>> GetByUsuarioAsync(Guid usuarioId, CancellationToken ct)
        {
            CalledUserIds.Add(usuarioId);
            return Task.FromResult(new System.Collections.Generic.List<NotificacionResult>());
        }

        public Task<bool> MarkAsReadAsync(Guid id, Guid usuarioId, CancellationToken ct) => Task.FromResult(true);
        public Task MarkAllAsReadAsync(Guid usuarioId, CancellationToken ct) => Task.CompletedTask;
        public Task<bool> DeleteAsync(Guid id, Guid usuarioId, CancellationToken ct) => Task.FromResult(true);
        public Task SubscribePushAsync(Guid usuarioId, string endpoint, string p256dh, string auth, CancellationToken ct) => Task.CompletedTask;
    }

    private sealed class FakeTimeProvider(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }
}
