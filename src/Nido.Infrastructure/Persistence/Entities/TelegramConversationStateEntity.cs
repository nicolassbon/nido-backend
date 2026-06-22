namespace Nido.Infrastructure.Persistence.Entities;

public sealed class TelegramConversationStateEntity
{
    public long ChatId { get; set; }

    public string MenuId { get; set; } = null!;

    public string? PayloadJson { get; set; }

    public DateTime LastInteractionAtUtc { get; set; }

    public DateTime ExpiresAtUtc { get; set; }
}
