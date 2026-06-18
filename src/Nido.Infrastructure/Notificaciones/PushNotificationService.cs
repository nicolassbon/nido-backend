using System;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Nido.Application.Common.Notifications;
using Nido.Infrastructure.Persistence;
using WebPush;

namespace Nido.Infrastructure.Notificaciones;

public sealed class PushNotificationService : IPushNotificationService
{
    private readonly NidoDbContext _db;
    private readonly IConfiguration _config;
    private readonly ILogger<PushNotificationService> _logger;

    public PushNotificationService(
        NidoDbContext db,
        IConfiguration config,
        ILogger<PushNotificationService> logger)
    {
        _db = db;
        _config = config;
        _logger = logger;
    }

    public async Task SendNotificationAsync(Guid usuarioId, string titulo, string mensaje, string? urlRedirect, CancellationToken ct)
    {
        var suscripciones = await _db.SuscripcionesPush
            .Where(s => s.UsuarioId == usuarioId)
            .ToListAsync(ct);

        if (suscripciones.Count == 0)
        {
            return;
        }

        var vapidPublicKey = _config["Vapid:PublicKey"] ?? _config["Vapid__PublicKey"];
        var vapidPrivateKey = _config["Vapid:PrivateKey"] ?? _config["Vapid__PrivateKey"];
        var vapidSubject = _config["Vapid:Subject"] ?? _config["Vapid__Subject"] ?? "mailto:nido.app.mailer@gmail.com";

        if (string.IsNullOrEmpty(vapidPublicKey) || string.IsNullOrEmpty(vapidPrivateKey))
        {
            _logger.LogWarning("VAPID Keys not configured in settings. Skipping push notification.");
            return;
        }

        var vapidDetails = new VapidDetails(vapidSubject, vapidPublicKey, vapidPrivateKey);
        var client = new WebPushClient();

        var payloadObj = new
        {
            titulo,
            mensaje,
            url = urlRedirect ?? "/"
        };
        var payloadJson = JsonSerializer.Serialize(payloadObj);

        foreach (var sub in suscripciones)
        {
            try
            {
                var pushSubscription = new WebPush.PushSubscription(sub.Endpoint, sub.P256dh, sub.Auth);
                await client.SendNotificationAsync(pushSubscription, payloadJson, vapidDetails);
            }
            catch (WebPushException ex) when (ex.StatusCode == System.Net.HttpStatusCode.Gone || ex.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                // Subscription has expired or is no longer valid; remove it from database
                _db.SuscripcionesPush.Remove(sub);
                _logger.LogInformation("Push subscription {Id} has expired or was not found (status {Status}). Removed from DB.", sub.Id, ex.StatusCode);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending push notification to subscription {Id}", sub.Id);
            }
        }

        if (_db.ChangeTracker.HasChanges())
        {
            await _db.SaveChangesAsync(ct);
        }
    }
}
