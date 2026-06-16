using System;
using System.Threading;
using System.Threading.Tasks;

namespace Nido.Application.Notificaciones;

public sealed record MarkNotificationAsReadCommand(Guid Id, Guid UsuarioId);

public sealed class MarkNotificationAsReadHandler(INotificacionesRepository repository)
{
    public Task<bool> Handle(MarkNotificationAsReadCommand command, CancellationToken ct) =>
        repository.MarkAsReadAsync(command.Id, command.UsuarioId, ct);
}
