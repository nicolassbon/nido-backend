using Microsoft.EntityFrameworkCore;
using Nido.Application.Telegram;
using Nido.Application.Telegram.Conversation;
using Nido.Infrastructure.Persistence;
using Nido.Infrastructure.Persistence.Entities;

namespace Nido.Infrastructure.Telegram.Conversation;

public sealed class PostgresTelegramConversationStateStore(
    NidoDbContext db,
    TelegramOptions options,
    TimeProvider? timeProvider = null) : ITelegramConversationStateStore
{
    private readonly TimeProvider _timeProvider = timeProvider ?? TimeProvider.System;

    public async Task<TelegramConversationState?> GetAsync(long chatId, CancellationToken ct)
    {
        var entity = await db.TelegramConversationStates
            .SingleOrDefaultAsync(x => x.ChatId == chatId, ct);

        if (entity is null)
        {
            return null;
        }

        var utcNow = _timeProvider.GetUtcNow().UtcDateTime;
        if (entity.ExpiresAtUtc <= utcNow)
        {
            db.TelegramConversationStates.Remove(entity);
            await db.SaveChangesAsync(ct);
            return null;
        }

        return new TelegramConversationState(
            entity.ChatId,
            entity.MenuId,
            entity.LastInteractionAtUtc,
            entity.PayloadJson);
    }

    public async Task SetAsync(TelegramConversationState state, CancellationToken ct)
    {
        var utcNow = _timeProvider.GetUtcNow().UtcDateTime;
        var expiresAtUtc = utcNow.AddMinutes(options.ConversationStateTtlMinutes);

        var entity = await db.TelegramConversationStates
            .SingleOrDefaultAsync(x => x.ChatId == state.ChatId, ct);

        if (entity is null)
        {
            entity = new TelegramConversationStateEntity { ChatId = state.ChatId };
            db.TelegramConversationStates.Add(entity);
        }

        entity.MenuId = state.MenuId;
        entity.PayloadJson = state.PayloadJson;
        entity.LastInteractionAtUtc = utcNow;
        entity.ExpiresAtUtc = expiresAtUtc;

        await db.SaveChangesAsync(ct);
    }

    public async Task ClearAsync(long chatId, CancellationToken ct)
    {
        var entity = await db.TelegramConversationStates
            .SingleOrDefaultAsync(x => x.ChatId == chatId, ct);

        if (entity is null)
        {
            return;
        }

        db.TelegramConversationStates.Remove(entity);
        await db.SaveChangesAsync(ct);
    }
}
