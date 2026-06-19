using System;
using System.Threading;
using System.Threading.Tasks;

namespace Nido.Application.Notificaciones;

public sealed record SubscribePushCommand(Guid UsuarioId, string Endpoint, string P256dh, string Auth);

public sealed class SubscribePushHandler(INotificacionesRepository repository)
{
    public Task Handle(SubscribePushCommand command, CancellationToken ct) =>
        repository.SubscribePushAsync(command.UsuarioId, command.Endpoint, command.P256dh, command.Auth, ct);
}
