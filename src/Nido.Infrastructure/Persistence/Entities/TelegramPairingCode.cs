namespace Nido.Infrastructure.Persistence.Entities;

public partial class TelegramPairingCode
{
    public Guid Id { get; set; }

    public Guid HogarId { get; set; }

    public Guid UsuarioId { get; set; }

    public string CodeHash { get; set; } = null!;

    public int Status { get; set; }

    public int AttemptCount { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime ExpiresAt { get; set; }

    public DateTime? ConsumedAt { get; set; }

    public DateTime? RevokedAt { get; set; }

    public virtual Hogare Hogar { get; set; } = null!;

    public virtual Usuario Usuario { get; set; } = null!;
}
