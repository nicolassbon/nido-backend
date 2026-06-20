using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Nido.Application.Telegram.Messaging;
using Nido.Infrastructure.Persistence;
using Nido.Infrastructure.Persistence.Entities;
using Nido.Infrastructure.Tareas;
using Nido.Tests.Shared;
using Xunit;

namespace Nido.Infrastructure.Tests.Telegram;

public sealed class TareaTelegramNotificationTests : IAsyncLifetime
{
    private readonly PostgresTestServer _server = PostgresTestServer.GetSharedAsync().GetAwaiter().GetResult();
    private PostgresTestDatabase _database = null!;
    private NidoDbContext _db = null!;
    private FakeTelegramNotificationBatcher _fakeBatcher = null!;
    private TareaRepository _sut = null!;

    public async Task InitializeAsync()
    {
        _database = await _server.CreateDatabaseAsync("tarea_telegram_notification_tests");

        var options = new DbContextOptionsBuilder<NidoDbContext>()
            .UseNpgsql(_database.ConnectionString)
            .ConfigureWarnings(w => w.Ignore(RelationalEventId.PendingModelChangesWarning))
            .Options;

        _db = new NidoDbContext(options);
        await _db.Database.MigrateAsync();

        _fakeBatcher = new FakeTelegramNotificationBatcher();
        _sut = new TareaRepository(_db, _fakeBatcher);
    }

    public async Task DisposeAsync()
    {
        await _db.DisposeAsync();
        await _database.DisposeAsync();
    }

    private async Task<(Guid HogarId, Guid CreatorId, Guid AssignedId, long ChatId)> SeedHogarAndUsersAsync(bool linkTelegram)
    {
        var hogarId = Guid.NewGuid();
        var creatorId = Guid.NewGuid();
        var assignedId = Guid.NewGuid();
        var chatId = 123456789L;

        _db.Hogares.Add(new Hogare
        {
            Id = hogarId,
            Nombre = "Test Hogar",
            CreatedAt = DateTime.UtcNow
        });

        _db.Usuarios.Add(new Usuario
        {
            Id = creatorId,
            Nombre = "Creator User",
            Email = $"creator-{Guid.NewGuid():N}@test.local",
            Sexo = "U",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        });

        _db.Usuarios.Add(new Usuario
        {
            Id = assignedId,
            Nombre = "Assigned User",
            Email = $"assigned-{Guid.NewGuid():N}@test.local",
            Sexo = "U",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        });

        if (linkTelegram)
        {
            _db.TelegramChatLinks.Add(new TelegramChatLink
            {
                Id = Guid.NewGuid(),
                ChatId = chatId,
                UsuarioId = assignedId,
                HogarId = hogarId,
                PairedAt = DateTime.UtcNow
            });
        }

        await _db.SaveChangesAsync();
        _db.ChangeTracker.Clear();

        return (hogarId, creatorId, assignedId, chatId);
    }

    [Fact]
    public async Task CreateAsync_WhenUserHasActiveTelegramLink_EnqueuesTelegramNotification()
    {
        // Arrange
        var (hogarId, creatorId, assignedId, chatId) = await SeedHogarAndUsersAsync(linkTelegram: true);

        // Act
        var result = await _sut.CreateAsync(
            hogarId,
            creatorId,
            "Test Task",
            "Test Description",
            DateTime.UtcNow.AddDays(1),
            assignedId,
            CancellationToken.None);

        // Assert
        Assert.Single(_fakeBatcher.EnqueuedEvents);
        var evt = _fakeBatcher.EnqueuedEvents[0];
        Assert.Equal(hogarId, evt.HogarId);
        Assert.Equal(chatId, evt.ChatId);
        Assert.Equal("asignacion_tarea", evt.MessageType);
        using (var doc = System.Text.Json.JsonDocument.Parse(evt.PayloadJson))
        {
            var text = doc.RootElement.GetProperty("text").GetString();
            Assert.Contains("Creator User te asignó la tarea \"Test Task\"", text);
        }
        Assert.False(evt.IsCritical);
    }

    [Fact]
    public async Task CreateAsync_WhenUserHasNoActiveTelegramLink_DoesNotEnqueueTelegramNotification()
    {
        // Arrange
        var (hogarId, creatorId, assignedId, _) = await SeedHogarAndUsersAsync(linkTelegram: false);

        // Act
        var result = await _sut.CreateAsync(
            hogarId,
            creatorId,
            "Test Task",
            "Test Description",
            DateTime.UtcNow.AddDays(1),
            assignedId,
            CancellationToken.None);

        // Assert
        Assert.Empty(_fakeBatcher.EnqueuedEvents);
    }

    [Fact]
    public async Task AsignarAsync_WhenUserHasActiveTelegramLink_EnqueuesTelegramNotification()
    {
        // Arrange
        var (hogarId, creatorId, assignedId, chatId) = await SeedHogarAndUsersAsync(linkTelegram: true);
        
        // Create task without assignment first
        var taskResult = await _sut.CreateAsync(hogarId, creatorId, "Unassigned Task", "Desc", null, null, CancellationToken.None);
        _fakeBatcher.EnqueuedEvents.Clear(); // Clear the enqueued events from task creation (it was unassigned anyway, but just in case)

        // Act
        await _sut.AsignarAsync(taskResult.Id, hogarId, assignedId, creatorId, CancellationToken.None);

        // Assert
        Assert.Single(_fakeBatcher.EnqueuedEvents);
        var evt = _fakeBatcher.EnqueuedEvents[0];
        Assert.Equal(hogarId, evt.HogarId);
        Assert.Equal(chatId, evt.ChatId);
        Assert.Equal("asignacion_tarea", evt.MessageType);
        using (var doc = System.Text.Json.JsonDocument.Parse(evt.PayloadJson))
        {
            var text = doc.RootElement.GetProperty("text").GetString();
            Assert.Contains("Creator User te asignó la tarea \"Unassigned Task\"", text);
        }
        Assert.False(evt.IsCritical);
    }

    [Fact]
    public async Task AsignarAsync_WhenSelfAssignment_DoesNotEnqueueTelegramNotification()
    {
        // Arrange
        var (hogarId, creatorId, _, chatId) = await SeedHogarAndUsersAsync(linkTelegram: false);
        
        // link the creator user
        _db.TelegramChatLinks.Add(new TelegramChatLink
        {
            Id = Guid.NewGuid(),
            ChatId = chatId,
            UsuarioId = creatorId,
            HogarId = hogarId,
            PairedAt = DateTime.UtcNow
        });
        await _db.SaveChangesAsync();
        _db.ChangeTracker.Clear();

        var taskResult = await _sut.CreateAsync(hogarId, creatorId, "Self Task", "Desc", null, null, CancellationToken.None);
        _fakeBatcher.EnqueuedEvents.Clear();

        // Act - assign to self (creator assigns to creator)
        await _sut.AsignarAsync(taskResult.Id, hogarId, creatorId, creatorId, CancellationToken.None);

        // Assert
        Assert.Empty(_fakeBatcher.EnqueuedEvents);
    }

    private sealed class FakeTelegramNotificationBatcher : ITelegramNotificationBatcher
    {
        public List<(Guid HogarId, long ChatId, string MessageType, string PayloadJson, bool IsCritical)> EnqueuedEvents { get; } = new();

        public Task EnqueueEventAsync(Guid hogarId, long chatId, string messageType, string payloadJson, bool isCritical, CancellationToken ct = default)
        {
            EnqueuedEvents.Add((hogarId, chatId, messageType, payloadJson, isCritical));
            return Task.CompletedTask;
        }

        public Task ProcessExpiredBatchesAsync(CancellationToken ct = default)
        {
            return Task.CompletedTask;
        }
    }
}
