using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Nido.Application.Notificaciones;

public sealed record GetNotificationsQuery(Guid UsuarioId);

public sealed class GetNotificationsHandler(INotificacionesRepository repository)
{
    public Task<List<NotificacionResult>> Handle(GetNotificationsQuery query, CancellationToken ct) =>
        repository.GetByUsuarioAsync(query.UsuarioId, ct);
}
