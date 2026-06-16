namespace Nido.Infrastructure.Persistence.Entities;

public partial class ProcessedTelegramUpdate
{
    public Guid Id { get; set; }

    public long UpdateId { get; set; }

    public string? UpdateHash { get; set; }

    public DateTime ProcessedAt { get; set; }
}
