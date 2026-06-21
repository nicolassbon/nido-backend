namespace Nido.Api.Contracts.Notificaciones;

public sealed record SubscribePushRequest(
    string Endpoint,
    string P256dh,
    string Auth
);
