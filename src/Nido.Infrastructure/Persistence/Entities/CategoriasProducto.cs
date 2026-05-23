using System;
using System.Collections.Generic;

namespace Nido.Infrastructure.Persistence.Entities;

public partial class CategoriasProducto
{
    public Guid Id { get; set; }

    public string Nombre { get; set; } = null!;

    public int? TtlDias { get; set; }

    public virtual ICollection<Producto> Productos { get; set; } = new List<Producto>();
}
