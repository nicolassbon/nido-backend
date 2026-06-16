namespace Nido.Infrastructure.Persistence.Entities;

public partial class TelegramOutboxMessage
{
    public Guid Id { get; set; }

    public Guid HogarId { get; set; }

    public long ChatId { get; set; }

    public string MessageType { get; set; } = null!;

    public string PayloadJson { get; set; } = null!;

    public int Status { get; set; }

    public int Attempts { get; set; }

    public DateTime? NextAttemptAt { get; set; }

    public DateTime? LockedUntil { get; set; }

    public Guid? BatchId { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual TelegramBatch? Batch { get; set; }
}
