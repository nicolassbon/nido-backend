using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Nido.Application.Notificaciones;

public interface INotificacionesRepository
{
    Task<List<NotificacionResult>> GetByUsuarioAsync(Guid usuarioId, CancellationToken ct);
    Task<bool> MarkAsReadAsync(Guid id, Guid usuarioId, CancellationToken ct);
    Task MarkAllAsReadAsync(Guid usuarioId, CancellationToken ct);
    Task<bool> DeleteAsync(Guid id, Guid usuarioId, CancellationToken ct);
}
