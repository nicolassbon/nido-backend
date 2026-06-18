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
        // Generar notificaciones para tareas vencidas asignadas a este usuario
        var hoy = DateTime.UtcNow.Date;
        var tareasVencidas = await db.Tareas
            .Where(t => t.Estado != "completada"
                     && t.FechaLimite.HasValue
                     && t.FechaLimite.Value.Date < hoy
                     && t.AsignacionesTareas.Any(a => a.UsuarioId == usuarioId))
            .ToListAsync(ct);

        foreach (var tarea in tareasVencidas)
        {
            var existeNotif = await db.Notificaciones.AnyAsync(n =>
                n.UsuarioId == usuarioId
                && n.ReferenciaTipo == "tarea"
                && n.ReferenciaId == tarea.Id
                && n.Tipo == "tarea_vencida", ct);

            if (!existeNotif)
            {
                db.Notificaciones.Add(new Notificacione
                {
                    Id = Guid.NewGuid(),
                    UsuarioId = usuarioId,
                    Tipo = "tarea_vencida",
                    ReferenciaTipo = "tarea",
                    ReferenciaId = tarea.Id,
                    Mensaje = $"La tarea \"{tarea.Titulo}\" se encuentra vencida",
                    Leida = false,
                    CreatedAt = DateTime.UtcNow
                });
            }
        }

        // Generar notificaciones para stock bajo y vencimiento de productos en los hogares del usuario
        var hogaresUsuario = await db.MiembrosHogars
            .Where(m => m.UsuarioId == usuarioId)
            .Select(m => m.HogarId)
            .ToListAsync(ct);

        var usuario = await db.Usuarios.FirstOrDefaultAsync(u => u.Id == usuarioId, ct);
        var diasAlerta = usuario?.AlertaVencimientoDias ?? 7;

        var stockItems = await db.StockHogars
            .Include(s => s.Producto)
            .Where(s => hogaresUsuario.Contains(s.HogarId))
            .ToListAsync(ct);

        var alertasActivas = new List<(Guid ProductoStockId, string Tipo)>();

        foreach (var stock in stockItems)
        {
            var productoNombre = stock.Producto?.Nombre ?? "Producto Desconocido";
            var ultimoCambioStock = stock.UpdatedAt ?? stock.CreatedAt;

            // 1. Alerta de Vencimiento
            if (stock.FechaVencimiento.HasValue)
            {
                var hoyDateOnly = DateOnly.FromDateTime(DateTime.UtcNow);
                if (stock.FechaVencimiento.Value < hoyDateOnly)
                {
                    alertasActivas.Add((stock.Id, "producto_vencido"));
                    await AsegurarNotificacionAsync(usuarioId, "producto_vencido", "alacena", stock.Id, $"El producto \"{productoNombre}\" se encuentra vencido", ultimoCambioStock, ct);
                }
                else if (stock.FechaVencimiento.Value <= hoyDateOnly.AddDays(diasAlerta))
                {
                    alertasActivas.Add((stock.Id, "producto_por_vencer"));
                    await AsegurarNotificacionAsync(usuarioId, "producto_por_vencer", "alacena", stock.Id, $"El producto \"{productoNombre}\" requiere atención por vencimiento", ultimoCambioStock, ct);
                }
            }

            // 2. Alerta de Stock Bajo (30% o menos restante, es decir, PorcentajeConsumido >= 70)
            if (stock.CantidadEnvases == 1 && stock.EstaAbierto && stock.PorcentajeConsumido >= 70)
            {
                alertasActivas.Add((stock.Id, "stock_bajo"));
                await AsegurarNotificacionAsync(usuarioId, "stock_bajo", "alacena", stock.Id, $"El stock del producto \"{productoNombre}\" es bajo", ultimoCambioStock, ct);
            }
        }

        // 3. Limpieza de alertas de alacena obsoletas
        var tiposAlacena = new[] { "producto_vencido", "producto_por_vencer", "stock_bajo" };
        var notificacionesAlacenaActivas = await db.Notificaciones
            .Where(n => n.UsuarioId == usuarioId
                     && n.ReferenciaTipo == "alacena"
                     && (n.Leida == false || !n.Leida.HasValue)
                     && n.Tipo != null
                     && tiposAlacena.Contains(n.Tipo))
            .ToListAsync(ct);

        foreach (var notif in notificacionesAlacenaActivas)
        {
            if (notif.ReferenciaId.HasValue)
            {
                var todaviaActiva = alertasActivas.Any(a => a.ProductoStockId == notif.ReferenciaId.Value && a.Tipo == notif.Tipo);
                if (!todaviaActiva)
                {
                    db.Notificaciones.Remove(notif);
                }
            }
        }

        if (db.ChangeTracker.HasChanges())
        {
            await db.SaveChangesAsync(ct);
        }

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

    private async Task AsegurarNotificacionAsync(
        Guid usuarioId,
        string tipo,
        string referenciaTipo,
        Guid referenciaId,
        string mensaje,
        DateTime ultimoCambioRef,
        CancellationToken ct)
    {
        var existeNotif = await db.Notificaciones.AnyAsync(n =>
            n.UsuarioId == usuarioId
            && n.ReferenciaTipo == referenciaTipo
            && n.ReferenciaId == referenciaId
            && n.Tipo == tipo
            && n.CreatedAt >= ultimoCambioRef, ct);

        if (!existeNotif)
        {
            db.Notificaciones.Add(new Notificacione
            {
                Id = Guid.NewGuid(),
                UsuarioId = usuarioId,
                Tipo = tipo,
                ReferenciaTipo = referenciaTipo,
                ReferenciaId = referenciaId,
                Mensaje = mensaje,
                Leida = false,
                CreatedAt = DateTime.UtcNow
            });
        }
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

    public async Task SubscribePushAsync(Guid usuarioId, string endpoint, string p256dh, string auth, CancellationToken ct)
    {
        var existing = await db.SuscripcionesPush
            .FirstOrDefaultAsync(s => s.UsuarioId == usuarioId && s.Endpoint == endpoint, ct);

        if (existing is null)
        {
            db.SuscripcionesPush.Add(new SuscripcionPush
            {
                Id = Guid.NewGuid(),
                UsuarioId = usuarioId,
                Endpoint = endpoint,
                P256dh = p256dh,
                Auth = auth,
                CreatedAt = DateTime.UtcNow
            });
        }
        else
        {
            existing.P256dh = p256dh;
            existing.Auth = auth;
        }

        await db.SaveChangesAsync(ct);
    }
}
