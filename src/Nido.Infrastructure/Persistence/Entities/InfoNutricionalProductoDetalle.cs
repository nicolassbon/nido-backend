using System;

namespace Nido.Infrastructure.Persistence.Entities;

public partial class InfoNutricionalProductoDetalle
{
    public Guid Id { get; set; }

    public Guid InfoNutricionalProductoId { get; set; }

    public string Nombre { get; set; } = null!;

    public decimal? Valor { get; set; }

    public string? Unidad { get; set; }

    public decimal? PorcentajeDiario { get; set; }

    public int Orden { get; set; }

    public virtual InfoNutricionalProducto InfoNutricionalProducto { get; set; } = null!;
}

