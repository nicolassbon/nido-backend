using System;

namespace Nido.Infrastructure.Persistence.Entities;

public partial class PresupuestoMensual
{
    public Guid Id { get; set; }
    public Guid HogarId { get; set; }
    public int Anio { get; set; }
    public int Mes { get; set; }
    public decimal Monto { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public virtual Hogare Hogar { get; set; } = null!;
}
