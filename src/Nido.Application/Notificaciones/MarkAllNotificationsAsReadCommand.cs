using System;
using System.Threading;
using System.Threading.Tasks;

namespace Nido.Application.Notificaciones;

public sealed record MarkAllNotificationsAsReadCommand(Guid UsuarioId);

public sealed class MarkAllNotificationsAsReadHandler(INotificacionesRepository repository)
{
    public async Task Handle(MarkAllNotificationsAsReadCommand command, CancellationToken ct) =>
        await repository.MarkAllAsReadAsync(command.UsuarioId, ct);
}
