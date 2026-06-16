using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Nido.Application.Notificaciones;
using Nido.Infrastructure.Persistence;
using Nido.Infrastructure.Persistence.Entities;

namespace Nido.Infrastructure.Notificaciones;

public sealed class NotificacionesRepository(NidoDbContext db) : INotificacionesRepository
{
    public async Task<List<NotificacionResult>> GetByUsuarioAsync(Guid usuarioId, CancellationToken ct)
    {
        var list = await db.Notificaciones
            .Where(n => n.UsuarioId == usuarioId)
            .OrderByDescending(n => n.CreatedAt)
            .ToListAsync(ct);

        return list.Select(MapToResult).ToList();
    }

    public async Task<bool> MarkAsReadAsync(Guid id, Guid usuarioId, CancellationToken ct)
    {
        var notif = await db.Notificaciones.FirstOrDefaultAsync(n => n.Id == id && n.UsuarioId == usuarioId, ct);
        if (notif is null) return false;

        notif.Leida = true;
        await db.SaveChangesAsync(ct);
        return true;
    }

    public async Task MarkAllAsReadAsync(Guid usuarioId, CancellationToken ct)
    {
        var notifs = await db.Notificaciones
            .Where(n => n.UsuarioId == usuarioId && (n.Leida == false || !n.Leida.HasValue))
            .ToListAsync(ct);

        foreach (var n in notifs)
        {
            n.Leida = true;
        }

        await db.SaveChangesAsync(ct);
    }

    public async Task<bool> DeleteAsync(Guid id, Guid usuarioId, CancellationToken ct)
    {
        var notif = await db.Notificaciones.FirstOrDefaultAsync(n => n.Id == id && n.UsuarioId == usuarioId, ct);
        if (notif is null) return false;

        db.Notificaciones.Remove(notif);
        await db.SaveChangesAsync(ct);
        return true;
    }

    private static NotificacionResult MapToResult(Notificacione n)
    {
        return new NotificacionResult(
            n.Id,
            n.UsuarioId,
            n.Tipo,
            n.Mensaje,
            n.Leida ?? false,
            n.ReferenciaId,
            n.ReferenciaTipo,
            n.CreatedAt);
    }
}
