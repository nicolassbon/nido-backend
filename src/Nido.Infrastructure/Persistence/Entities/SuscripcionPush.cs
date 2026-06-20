using System;

namespace Nido.Infrastructure.Persistence.Entities;

public partial class SuscripcionPush
{
    public Guid Id { get; set; }

    public Guid UsuarioId { get; set; }

    public string Endpoint { get; set; } = null!;

    public string P256dh { get; set; } = null!;

    public string Auth { get; set; } = null!;

    public DateTime CreatedAt { get; set; }

    public virtual Usuario Usuario { get; set; } = null!;
}
