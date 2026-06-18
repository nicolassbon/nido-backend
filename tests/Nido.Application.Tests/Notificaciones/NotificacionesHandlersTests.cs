using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Nido.Application.Notificaciones;
using Xunit;

namespace Nido.Application.Tests.Notificaciones;

public sealed class NotificacionesHandlersTests
{
    [Fact]
    public async Task GetNotificationsHandler_ReturnsUserNotifications()
    {
        var usuarioId = Guid.NewGuid();
        var notifications = new List<NotificacionResult>
        {
            new(Guid.NewGuid(), usuarioId, "tipo1", "mensaje1", false, null, null, DateTime.UtcNow),
            new(Guid.NewGuid(), usuarioId, "tipo2", "mensaje2", true, null, null, DateTime.UtcNow)
        };
        var repo = new FakeNotificacionesRepository { Notifications = notifications };
        var handler = new GetNotificationsHandler(repo);

        var result = await handler.Handle(new GetNotificationsQuery(usuarioId), CancellationToken.None);

        Assert.Equal(2, result.Count);
        Assert.Equal("mensaje1", result[0].Mensaje);
    }

    [Fact]
    public async Task MarkNotificationAsReadHandler_ValidId_ReturnsTrue()
    {
        var id = Guid.NewGuid();
        var usuarioId = Guid.NewGuid();
        var repo = new FakeNotificacionesRepository();
        repo.Notifications.Add(new NotificacionResult(id, usuarioId, "tipo", "msg", false, null, null, DateTime.UtcNow));
        var handler = new MarkNotificationAsReadHandler(repo);

        var result = await handler.Handle(new MarkNotificationAsReadCommand(id, usuarioId), CancellationToken.None);

        Assert.True(result);
        Assert.True(repo.Notifications[0].Leida);
    }

    [Fact]
    public async Task MarkNotificationAsReadHandler_InvalidId_ReturnsFalse()
    {
        var repo = new FakeNotificacionesRepository();
        var handler = new MarkNotificationAsReadHandler(repo);

        var result = await handler.Handle(new MarkNotificationAsReadCommand(Guid.NewGuid(), Guid.NewGuid()), CancellationToken.None);

        Assert.False(result);
    }

    [Fact]
    public async Task MarkAllNotificationsAsReadHandler_MarksAllAsRead()
    {
        var usuarioId = Guid.NewGuid();
        var repo = new FakeNotificacionesRepository();
        repo.Notifications.Add(new NotificacionResult(Guid.NewGuid(), usuarioId, "tipo", "msg1", false, null, null, DateTime.UtcNow));
        repo.Notifications.Add(new NotificacionResult(Guid.NewGuid(), usuarioId, "tipo", "msg2", false, null, null, DateTime.UtcNow));
        var handler = new MarkAllNotificationsAsReadHandler(repo);

        await handler.Handle(new MarkAllNotificationsAsReadCommand(usuarioId), CancellationToken.None);

        Assert.All(repo.Notifications, n => Assert.True(n.Leida));
    }

    [Fact]
    public async Task DeleteNotificationHandler_ValidId_ReturnsTrueAndDeletes()
    {
        var id = Guid.NewGuid();
        var usuarioId = Guid.NewGuid();
        var repo = new FakeNotificacionesRepository();
        repo.Notifications.Add(new NotificacionResult(id, usuarioId, "tipo", "msg", false, null, null, DateTime.UtcNow));
        var handler = new DeleteNotificationHandler(repo);

        var result = await handler.Handle(new DeleteNotificationCommand(id, usuarioId), CancellationToken.None);

        Assert.True(result);
        Assert.Empty(repo.Notifications);
    }

    [Fact]
    public async Task DeleteNotificationHandler_InvalidId_ReturnsFalse()
    {
        var repo = new FakeNotificacionesRepository();
        var handler = new DeleteNotificationHandler(repo);

        var result = await handler.Handle(new DeleteNotificationCommand(Guid.NewGuid(), Guid.NewGuid()), CancellationToken.None);

        Assert.False(result);
    }

    private sealed class FakeNotificacionesRepository : INotificacionesRepository
    {
        public List<NotificacionResult> Notifications { get; set; } = [];

        public Task<List<NotificacionResult>> GetByUsuarioAsync(Guid usuarioId, CancellationToken ct)
        {
            var userNotifs = Notifications.Where(n => n.UsuarioId == usuarioId).ToList();
            return Task.FromResult(userNotifs);
        }

        public Task<bool> MarkAsReadAsync(Guid id, Guid usuarioId, CancellationToken ct)
        {
            var notif = Notifications.FirstOrDefault(n => n.Id == id && n.UsuarioId == usuarioId);
            if (notif == null) return Task.FromResult(false);

            // Replace the record to update Leida to true
            Notifications.Remove(notif);
            Notifications.Add(notif with { Leida = true });
            return Task.FromResult(true);
        }

        public Task MarkAllAsReadAsync(Guid usuarioId, CancellationToken ct)
        {
            var userNotifs = Notifications.Where(n => n.UsuarioId == usuarioId).ToList();
            foreach (var n in userNotifs)
            {
                Notifications.Remove(n);
                Notifications.Add(n with { Leida = true });
            }
            return Task.CompletedTask;
        }

        public Task<bool> DeleteAsync(Guid id, Guid usuarioId, CancellationToken ct)
        {
            var notif = Notifications.FirstOrDefault(n => n.Id == id && n.UsuarioId == usuarioId);
            if (notif == null) return Task.FromResult(false);

            Notifications.Remove(notif);
            return Task.FromResult(true);
        }

        public Task SubscribePushAsync(Guid usuarioId, string endpoint, string p256dh, string auth, CancellationToken ct)
        {
            return Task.CompletedTask;
        }
    }
}
