using System;
using System.Collections.Generic;

namespace Nido.Infrastructure.Persistence.Entities;

public partial class Gasto
{
    public Guid Id { get; set; }

    public Guid HogarId { get; set; }

    public Guid PagadoPor { get; set; }

    public decimal Monto { get; set; }

    public string? Descripcion { get; set; }

    public string? Categoria { get; set; }

    public DateOnly Fecha { get; set; }

    public Guid? FacturaId { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual Hogare Hogar { get; set; } = null!;

    public virtual Usuario PagadoPorNavigation { get; set; } = null!;
}
