using Nido.Application.Telegram.Conversation;

namespace Nido.Application.Telegram.Pairing;

public sealed class UnlinkTelegramChatHandler(ITelegramPairingRepository repository, ITelegramConversationStateStore stateStore)
{
    public UnlinkTelegramChatHandler(ITelegramPairingRepository repository)
        : this(repository, new NoOpTelegramConversationStateStore())
    {
    }

    public async Task<UnlinkTelegramChatResult> HandleAsync(UnlinkTelegramChatCommand command, CancellationToken ct)
    {
        var result = await repository.UnlinkChatAsync(command.ChatId, ct);

        try
        {
            await stateStore.ClearAsync(command.ChatId, ct);
        }
        catch
        {
            // Best effort cleanup: unlink result remains the source of truth.
        }

        return result;
    }

    private sealed class NoOpTelegramConversationStateStore : ITelegramConversationStateStore
    {
        public Task<TelegramConversationState?> GetAsync(long chatId, CancellationToken ct) => Task.FromResult<TelegramConversationState?>(null);
        public Task SetAsync(TelegramConversationState state, CancellationToken ct) => Task.CompletedTask;
        public Task ClearAsync(long chatId, CancellationToken ct) => Task.CompletedTask;
    }
}
