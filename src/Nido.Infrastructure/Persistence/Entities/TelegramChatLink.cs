namespace Nido.Infrastructure.Persistence.Entities;

public partial class TelegramChatLink
{
    public Guid Id { get; set; }

    public long ChatId { get; set; }

    public Guid UsuarioId { get; set; }

    public Guid HogarId { get; set; }

    public DateTime PairedAt { get; set; }

    public DateTime? UnpairedAt { get; set; }

    public virtual Usuario Usuario { get; set; } = null!;

    public virtual Hogare Hogar { get; set; } = null!;
}
