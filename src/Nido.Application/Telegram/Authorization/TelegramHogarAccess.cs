using System;
using System.Threading;
using System.Threading.Tasks;

namespace Nido.Application.Telegram.Authorization;

public interface ITelegramHogarAccess
{
    Task<TelegramChatLinkSnapshot?> GetActiveLinkAsync(long chatId, CancellationToken ct);

    Task<bool> IsUserCurrentMemberAsync(Guid usuarioId, Guid hogarId, CancellationToken ct);

    Task<bool> IsUserAssignedToTaskAsync(Guid usuarioId, Guid tareaId, Guid hogarId, CancellationToken ct);
}

public sealed record TelegramChatLinkSnapshot(
    long ChatId,
    Guid UsuarioId,
    Guid HogarId,
    DateTime? PairedAt,
    DateTime? UnpairedAt);
