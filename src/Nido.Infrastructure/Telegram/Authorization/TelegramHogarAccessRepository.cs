using Microsoft.EntityFrameworkCore;
using Nido.Application.Telegram.Authorization;
using Nido.Infrastructure.Persistence;

namespace Nido.Infrastructure.Telegram.Authorization;

public sealed class TelegramHogarAccessRepository(NidoDbContext dbContext) : ITelegramHogarAccess
{
    public async Task<TelegramChatLinkSnapshot?> GetActiveLinkAsync(long chatId, CancellationToken ct)
    {
        return await dbContext.TelegramChatLinks
            .AsNoTracking()
            .Where(link => link.ChatId == chatId && link.UnpairedAt == null)
            .Select(link => new TelegramChatLinkSnapshot(
                link.ChatId,
                link.UsuarioId,
                link.HogarId,
                link.PairedAt,
                link.UnpairedAt))
            .SingleOrDefaultAsync(ct);
    }

    public Task<bool> IsUserCurrentMemberAsync(Guid usuarioId, Guid hogarId, CancellationToken ct)
        => dbContext.MiembrosHogars.AnyAsync(
            member => member.UsuarioId == usuarioId
                && member.HogarId == hogarId
                && member.NombreRepresentado == null,
            ct);

    public Task<bool> IsUserAssignedToTaskAsync(Guid usuarioId, Guid tareaId, Guid hogarId, CancellationToken ct)
        => dbContext.Tareas.AnyAsync(
            tarea => tarea.Id == tareaId
                && tarea.HogarId == hogarId
                && tarea.AsignacionesTareas.Any(asignacion => asignacion.UsuarioId == usuarioId),
            ct);

    public Task<bool> IsUserAssignedToPendingTaskAsync(Guid usuarioId, Guid tareaId, Guid hogarId, CancellationToken ct)
        => dbContext.Tareas.AnyAsync(
            tarea => tarea.Id == tareaId
                && tarea.HogarId == hogarId
                && tarea.Estado != "completada"
                && tarea.AsignacionesTareas.Any(asignacion => asignacion.UsuarioId == usuarioId),
            ct);
}
