using System;
using System.Threading;
using System.Threading.Tasks;

namespace Nido.Application.Notificaciones;

public sealed record DeleteNotificationCommand(Guid Id, Guid UsuarioId);

public sealed class DeleteNotificationHandler(INotificacionesRepository repository)
{
    public Task<bool> Handle(DeleteNotificationCommand command, CancellationToken ct) =>
        repository.DeleteAsync(command.Id, command.UsuarioId, ct);
}
