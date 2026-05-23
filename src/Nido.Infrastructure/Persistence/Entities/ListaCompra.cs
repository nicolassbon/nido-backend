using System;
using System.Collections.Generic;

namespace Nido.Infrastructure.Persistence.Entities;

public partial class ListaCompra
{
    public Guid Id { get; set; }

    public Guid HogarId { get; set; }

    public Guid ProductoId { get; set; }

    public Guid AgregadoPor { get; set; }

    public decimal? Cantidad { get; set; }

    public string? Unidad { get; set; }

    public bool? Comprado { get; set; }

    public bool? AgregadoAlInventario { get; set; }

    public virtual Usuario AgregadoPorNavigation { get; set; } = null!;

    public virtual Hogare Hogar { get; set; } = null!;

    public virtual Producto Producto { get; set; } = null!;
}
