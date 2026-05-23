using System;
using System.Collections.Generic;

namespace Nido.Infrastructure.Persistence.Entities;

public partial class InfoNutricionalRecetum
{
    public Guid Id { get; set; }

    public Guid RecetaId { get; set; }

    public decimal? Calorias { get; set; }

    public decimal? Proteinas { get; set; }

    public decimal? Carbohidratos { get; set; }

    public decimal? Grasas { get; set; }

    public virtual Receta Receta { get; set; } = null!;
}
