namespace Nido.Application.Telegram.Conversation;

public sealed record TelegramConversationState(
    long ChatId,
    string MenuId,
    DateTime LastInteractionAtUtc,
    string? PayloadJson);

public interface ITelegramConversationStateStore
{
    Task<TelegramConversationState?> GetAsync(long chatId, CancellationToken ct);
    Task SetAsync(TelegramConversationState state, CancellationToken ct);
    Task ClearAsync(long chatId, CancellationToken ct);
}
