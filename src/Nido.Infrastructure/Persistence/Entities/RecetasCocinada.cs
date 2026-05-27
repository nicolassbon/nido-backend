using System;
using System.Collections.Generic;

namespace Nido.Infrastructure.Persistence.Entities;

public partial class RecetasCocinada
{
    public Guid Id { get; set; }

    public Guid HogarId { get; set; }

    public Guid RecetaId { get; set; }

    public Guid CocinadoPor { get; set; }

    public int? PorcionesCocinadas { get; set; }

    public DateTime Fecha { get; set; }

    public virtual Usuario CocinadoPorNavigation { get; set; } = null!;

    public virtual Hogare Hogar { get; set; } = null!;

    public virtual Receta Receta { get; set; } = null!;
}
