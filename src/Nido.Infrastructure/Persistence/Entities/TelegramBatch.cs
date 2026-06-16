namespace Nido.Infrastructure.Persistence.Entities;

public partial class TelegramBatch
{
    public Guid Id { get; set; }

    public int Status { get; set; }

    public int Attempts { get; set; }

    public DateTime? NextAttemptAt { get; set; }

    public DateTime CreatedAt { get; set; }
}
