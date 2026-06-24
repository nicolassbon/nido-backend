using System;

namespace Nido.Infrastructure.Persistence.Entities;

public partial class RecetaGuardadaHogar
{
    public Guid HogarId { get; set; }

    public Guid RecetaId { get; set; }

    public Guid GuardadaPor { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual Hogare Hogar { get; set; } = null!;

    public virtual Receta Receta { get; set; } = null!;

    public virtual Usuario GuardadaPorNavigation { get; set; } = null!;
}
