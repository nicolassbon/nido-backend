namespace Nido.Application.Common.Notifications;

public interface IPushNotificationService
{
    Task SendNotificationAsync(Guid usuarioId, string titulo, string mensaje, string? urlRedirect, CancellationToken ct);
}
