namespace Nido.Infrastructure.Persistence.Entities;

public partial class PasswordResetToken
{
    public Guid Id { get; set; }

    public Guid UsuarioId { get; set; }

    public string TokenHash { get; set; } = null!;

    public DateTime ExpiresAt { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UsedAt { get; set; }

    public virtual Usuario Usuario { get; set; } = null!;
}
