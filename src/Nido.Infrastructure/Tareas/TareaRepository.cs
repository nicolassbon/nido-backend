using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Nido.Application.Tareas;
using Nido.Application.Telegram.Messaging;
using Nido.Infrastructure.Persistence;
using Nido.Infrastructure.Persistence.Entities;

namespace Nido.Infrastructure.Tareas;

public sealed class TareaRepository(
    NidoDbContext db,
    ITelegramNotificationBatcher batcher,
    ILogger<TareaRepository> logger,
    IConfiguration? configuration = null) : ITareaRepository
{
    public async Task<List<TareaResult>> GetByHogarAsync(Guid hogarId, CancellationToken ct)
    {
        var tareas = await db.Tareas
            .Include(t => t.CreadoPorNavigation)
            .Include(t => t.CompletadoPorNavigation)
            .Include(t => t.AsignacionesTareas)
                .ThenInclude(a => a.Usuario)
            .Where(t => t.HogarId == hogarId)
            .OrderByDescending(t => t.CreatedAt)
            .ToListAsync(ct);

        return tareas.Select(MapToResult).ToList();
    }

    public async Task<List<TareaResult>> GetByAsignadoAsync(Guid hogarId, Guid usuarioId, CancellationToken ct)
    {
        var tareas = await db.Tareas
            .Include(t => t.CreadoPorNavigation)
            .Include(t => t.CompletadoPorNavigation)
            .Include(t => t.AsignacionesTareas)
                .ThenInclude(a => a.Usuario)
            .Where(t => t.HogarId == hogarId &&
                        t.AsignacionesTareas.Any(a => a.UsuarioId == usuarioId) &&
                        t.Estado != "completada")
            .OrderBy(t => t.FechaLimite)
            .ThenByDescending(t => t.CreatedAt)
            .ToListAsync(ct);

        return tareas.Select(MapToResult).ToList();
    }

    public async Task<TareaResult?> GetByIdAsync(Guid id, Guid hogarId, CancellationToken ct)
    {
        var tarea = await db.Tareas
            .Include(t => t.CreadoPorNavigation)
            .Include(t => t.CompletadoPorNavigation)
            .Include(t => t.AsignacionesTareas)
                .ThenInclude(a => a.Usuario)
            .FirstOrDefaultAsync(t => t.Id == id && t.HogarId == hogarId, ct);

        return tarea is null ? null : MapToResult(tarea);
    }

    public async Task<TareaResult> CreateAsync(Guid hogarId, Guid creadoPor, string titulo, string? descripcion, DateTime? fechaLimite, Guid? asignadoA, CancellationToken ct)
    {
        var tarea = new Tarea
        {
            HogarId = hogarId,
            CreadoPor = creadoPor,
            Titulo = titulo,
            Descripcion = descripcion,
            Estado = "pendiente",
            FechaLimite = fechaLimite,
            CreatedAt = DateTime.UtcNow,
        };

        db.Tareas.Add(tarea);
        await db.SaveChangesAsync(ct);

        if (asignadoA.HasValue)
        {
            db.AsignacionesTareas.Add(new AsignacionesTarea
            {
                TareaId = tarea.Id,
                UsuarioId = asignadoA.Value,
                FechaAsignacion = DateTime.UtcNow,
            });

            var creador = await db.Usuarios.FirstOrDefaultAsync(u => u.Id == creadoPor, ct);
            var creadorNombre = creador?.Nombre ?? "Alguien";
            var messageText = $"{creadorNombre} te asignó la tarea \"{titulo}\"";

            db.Notificaciones.Add(new Notificacione
            {
                UsuarioId = asignadoA.Value,
                Tipo = "asignacion_tarea",
                ReferenciaTipo = "tarea",
                ReferenciaId = tarea.Id,
                Mensaje = messageText,
                Leida = false,
                CreatedAt = DateTime.UtcNow,
            });
            await db.SaveChangesAsync(ct);

            var activeLink = await GetActiveTelegramLinkForCurrentMemberAsync(asignadoA.Value, hogarId, ct);
            if (activeLink != null)
            {
                var payloadJson = BuildTelegramPayload(messageText, tarea.Id);
                await TryEnqueueTelegramNotificationAsync(
                    hogarId,
                    activeLink.ChatId,
                    "asignacion_tarea",
                    payloadJson,
                    ct);
            }
        }

        return (await GetByIdAsync(tarea.Id, hogarId, ct))!;
    }

    public async Task<TareaResult?> UpdateAsync(Guid id, Guid hogarId, string? titulo, string? descripcion, DateTime? fechaLimite, string? estado, CancellationToken ct)
    {
        var tarea = await db.Tareas.FirstOrDefaultAsync(t => t.Id == id && t.HogarId == hogarId, ct);
        if (tarea is null) return null;

        if (titulo is not null) tarea.Titulo = titulo;
        if (descripcion is not null) tarea.Descripcion = descripcion;
        if (fechaLimite.HasValue) tarea.FechaLimite = fechaLimite;
        if (estado is not null)
        {
            tarea.Estado = estado;
            if (estado != "completada") { tarea.CompletadoPor = null; tarea.FechaCompletado = null; }
        }

        await db.SaveChangesAsync(ct);
        return await GetByIdAsync(id, hogarId, ct);
    }

    public async Task<TareaResult?> CompletarAsync(Guid id, Guid hogarId, Guid completadoPor, CancellationToken ct)
    {
        var tarea = await db.Tareas.FirstOrDefaultAsync(t => t.Id == id && t.HogarId == hogarId, ct);
        if (tarea is null) return null;

        tarea.Estado = "completada";
        tarea.CompletadoPor = completadoPor;
        tarea.FechaCompletado = DateTime.UtcNow;

        await db.SaveChangesAsync(ct);
        return await GetByIdAsync(id, hogarId, ct);
    }

    public async Task<TareaResult?> AsignarAsync(Guid id, Guid hogarId, Guid? usuarioId, Guid asignadoPor, CancellationToken ct)
    {
        var tarea = await db.Tareas
            .Include(t => t.AsignacionesTareas)
            .FirstOrDefaultAsync(t => t.Id == id && t.HogarId == hogarId, ct);
        if (tarea is null) return null;

        db.AsignacionesTareas.RemoveRange(tarea.AsignacionesTareas);

        string? messageText = null;

        if (usuarioId.HasValue)
        {
            db.AsignacionesTareas.Add(new AsignacionesTarea
            {
                TareaId = tarea.Id,
                UsuarioId = usuarioId.Value,
                FechaAsignacion = DateTime.UtcNow,
            });
            if (usuarioId.Value != asignadoPor)
            {
                var asignador = await db.Usuarios.FirstOrDefaultAsync(u => u.Id == asignadoPor, ct);
                var asignadorNombre = asignador?.Nombre ?? "Alguien";
                messageText = $"{asignadorNombre} te asignó la tarea \"{tarea.Titulo}\"";

                db.Notificaciones.Add(new Notificacione
                {
                    UsuarioId = usuarioId.Value,
                    Tipo = "asignacion_tarea",
                    ReferenciaTipo = "tarea",
                    ReferenciaId = tarea.Id,
                    Mensaje = messageText,
                    Leida = false,
                    CreatedAt = DateTime.UtcNow,
                });
            }
        }

        await db.SaveChangesAsync(ct);

        if (usuarioId.HasValue && messageText != null)
        {
            var activeLink = await GetActiveTelegramLinkForCurrentMemberAsync(usuarioId.Value, hogarId, ct);
            if (activeLink != null)
            {
                var payloadJson = BuildTelegramPayload(messageText, tarea.Id);
                await TryEnqueueTelegramNotificationAsync(
                    hogarId,
                    activeLink.ChatId,
                    "asignacion_tarea",
                    payloadJson,
                    ct);
            }
        }

        return await GetByIdAsync(id, hogarId, ct);
    }

    public async Task<bool> DeleteAsync(Guid id, Guid hogarId, CancellationToken ct)
    {
        var tarea = await db.Tareas
            .Include(t => t.AsignacionesTareas)
            .FirstOrDefaultAsync(t => t.Id == id && t.HogarId == hogarId, ct);
        if (tarea is null) return false;

        db.AsignacionesTareas.RemoveRange(tarea.AsignacionesTareas);

        var notifs = await db.Notificaciones
            .Where(n => n.ReferenciaTipo == "tarea" && n.ReferenciaId == id)
            .ToListAsync(ct);
        db.Notificaciones.RemoveRange(notifs);

        db.Tareas.Remove(tarea);
        await db.SaveChangesAsync(ct);
        return true;
    }

    public async Task<List<DistribucionDiaResult>> GetDistribucionSemanalAsync(Guid hogarId, int utcOffsetMinutes, CancellationToken ct)
    {
        // Calcular el lunes de la semana actual en hora local del cliente
        var localNow = DateTime.UtcNow.AddMinutes(-utcOffsetMinutes);
        var localHoy = localNow.Date;
        var dow = (int)localHoy.DayOfWeek; // 0=domingo
        var inicioSemanaLocal = dow == 0 ? localHoy.AddDays(-6) : localHoy.AddDays(-(dow - 1));
        var finSemanaLocal = inicioSemanaLocal.AddDays(7);

        // Traemos todas las completadas del hogar sin filtrar por fecha (filtramos en memoria con hora local)
        var tareasCompletadas = await db.Tareas
            .Where(t => t.HogarId == hogarId && t.Estado == "completada" && t.FechaCompletado.HasValue)
            .Select(t => new
            {
                AsignadoA = t.AsignacionesTareas
                    .OrderByDescending(a => a.FechaAsignacion)
                    .Select(a => (Guid?)a.UsuarioId)
                    .FirstOrDefault(),
                t.CompletadoPor,
                t.FechaCompletado,
            })
            .ToListAsync(ct);

        var miembros = await db.MiembrosHogars
            .Include(m => m.Usuario)
            .Where(m => m.HogarId == hogarId && m.NombreRepresentado == null)
            .Select(m => new { m.UsuarioId, Nombre = m.Usuario.Nombre })
            .ToListAsync(ct);

        // Convertir FechaCompletado a hora local para agrupar correctamente
        var tareasConFechaLocal = tareasCompletadas
            .Select(t => new
            {
                t.AsignadoA,
                t.CompletadoPor,
                FechaLocal = t.FechaCompletado!.Value.AddMinutes(-utcOffsetMinutes).Date,
            })
            .Where(t => t.FechaLocal >= inicioSemanaLocal && t.FechaLocal < finSemanaLocal)
            .ToList();

        var diasSemana = new[] { "Lun", "Mar", "Mier", "Jue", "Vie", "Sab", "Dom" };
        var result = new List<DistribucionDiaResult>();

        for (int i = 0; i < 7; i++)
        {
            var diaLocal = inicioSemanaLocal.AddDays(i);
            var tareasDelDia = tareasConFechaLocal.Where(t => t.FechaLocal == diaLocal).ToList();
            var miembrosDistribucion = miembros
                .Select(m => new MiembroDistribucionResult(m.UsuarioId, m.Nombre,
                    tareasDelDia.Count(t => (t.AsignadoA ?? t.CompletadoPor) == m.UsuarioId)))
                .ToList();
            result.Add(new DistribucionDiaResult(diasSemana[i], diaLocal, miembrosDistribucion));
        }

        return result;
    }

    private async Task TryEnqueueTelegramNotificationAsync(
        Guid hogarId,
        long chatId,
        string messageType,
        string payloadJson,
        CancellationToken ct)
    {
        try
        {
            await batcher.EnqueueEventAsync(
                hogarId,
                chatId,
                messageType,
                payloadJson,
                isCritical: false,
                ct);
        }
        catch (Exception ex)
        {
            logger.LogWarning(
                ex,
                "Telegram task notification enqueue failed for hogar {HogarId}, chat {ChatId}, type {MessageType}.",
                hogarId,
                chatId,
                messageType);
        }
    }

    private async Task<TelegramChatLink?> GetActiveTelegramLinkForCurrentMemberAsync(
        Guid usuarioId,
        Guid hogarId,
        CancellationToken ct)
    {
        var activeLink = await db.TelegramChatLinks
            .FirstOrDefaultAsync(x => x.UsuarioId == usuarioId && x.HogarId == hogarId && x.UnpairedAt == null, ct);

        if (activeLink is null)
        {
            return null;
        }

        var hasActiveMembership = await db.MiembrosHogars
            .AnyAsync(x => x.UsuarioId == usuarioId && x.HogarId == hogarId, ct);

        if (hasActiveMembership)
        {
            return activeLink;
        }

        activeLink.UnpairedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);

        logger.LogInformation(
            "Unpaired stale Telegram link for user {UsuarioId} in hogar {HogarId} before sending task notifications.",
            usuarioId,
            hogarId);

        return null;
    }

    private string BuildTelegramPayload(string messageText, Guid tareaId)
    {
        var frontendBaseUrl = configuration?["Frontend:BaseUrl"] ?? "http://localhost:4200";
        var redirectUrl = $"{frontendBaseUrl.TrimEnd('/')}/tareas?taskId={tareaId}";
        var escapedMessage = System.Net.WebUtility.HtmlEncode(messageText);
        var escapedUrl = System.Net.WebUtility.HtmlEncode(redirectUrl);
        var formattedText = $"{escapedMessage}\n\n👉 <a href=\"{escapedUrl}\">Ver en Nido</a>";

        return JsonSerializer.Serialize(new
        {
            text = formattedText,
            parse_mode = "HTML"
        });
    }

    private static TareaResult MapToResult(Tarea t)
    {
        var asignacion = t.AsignacionesTareas
            .OrderByDescending(a => a.FechaAsignacion)
            .FirstOrDefault();

        return new TareaResult(
            t.Id, t.HogarId, t.Titulo, t.Descripcion, t.Estado ?? "pendiente",
            t.FechaLimite, t.FechaCompletado, t.CreadoPor, t.CreadoPorNavigation.Nombre,
            t.CompletadoPor, t.CompletadoPorNavigation?.Nombre,
            asignacion is null ? null : new AsignacionResult(
                asignacion.UsuarioId, asignacion.Usuario.Nombre, asignacion.Usuario.FotoStorageKey),
            t.CreatedAt);
    }
}
