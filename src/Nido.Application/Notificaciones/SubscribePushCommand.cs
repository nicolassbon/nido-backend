using System;
using System.Collections.Frozen;
using System.Threading;
using System.Threading.Tasks;

namespace Nido.Application.Notificaciones;

public sealed record SubscribePushCommand(Guid UsuarioId, string Endpoint, string P256dh, string Auth);

public sealed class InvalidPushEndpointException(string message) : Exception(message);

public sealed class SubscribePushHandler(INotificacionesRepository repository)
{
    private static readonly FrozenSet<string> AllowedDomains = new[]
    {
        // Google / Firebase Cloud Messaging
        "googleapis.com",
        // Mozilla Push Service
        "mozilla.com",
        "mozilla.org",
        "services.mozilla.com",
        // Apple Push Notification Service
        "apple.com",
        "web.push.apple.com",
        // Microsoft WNS
        "notify.windows.com",
        // Generic VAPID push relays used by browsers
        "pushpad.xyz",
        "onesignal.com",
    }.ToFrozenSet(StringComparer.OrdinalIgnoreCase);

    public Task Handle(SubscribePushCommand command, CancellationToken ct)
    {
        ValidateEndpoint(command.Endpoint);
        return repository.SubscribePushAsync(command.UsuarioId, command.Endpoint, command.P256dh, command.Auth, ct);
    }

    public static void ValidateEndpoint(string endpoint)
    {
        if (!Uri.TryCreate(endpoint, UriKind.Absolute, out var uri))
        {
            throw new InvalidPushEndpointException("Push endpoint is not a valid absolute URI.");
        }

        if (!string.Equals(uri.Scheme, "https", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidPushEndpointException("Push endpoint must use the HTTPS scheme.");
        }

        var host = uri.Host;
        foreach (var domain in AllowedDomains)
        {
            // Exact match (e.g. host == "notify.windows.com")
            if (string.Equals(host, domain, StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            // Subdomain match (e.g. "fcm.googleapis.com" ends with ".googleapis.com")
            if (host.EndsWith("." + domain, StringComparison.OrdinalIgnoreCase))
            {
                return;
            }
        }

        throw new InvalidPushEndpointException(
            $"Push endpoint host '{host}' is not in the list of allowed push service providers.");
    }
}
