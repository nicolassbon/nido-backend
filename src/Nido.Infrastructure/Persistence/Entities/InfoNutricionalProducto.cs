using System;
using System.Collections.Generic;

namespace Nido.Infrastructure.Persistence.Entities;

public partial class InfoNutricionalProducto
{
    public Guid Id { get; set; }

    public Guid ProductoId { get; set; }

    public decimal? Calorias { get; set; }

    public decimal? Proteinas { get; set; }

    public decimal? Carbohidratos { get; set; }

    public decimal? Grasas { get; set; }

    public virtual Producto Producto { get; set; } = null!;
}
