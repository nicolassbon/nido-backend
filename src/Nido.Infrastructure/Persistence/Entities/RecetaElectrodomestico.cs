using System;
using System.Collections.Generic;

namespace Nido.Infrastructure.Persistence.Entities;

public partial class RecetaElectrodomestico
{
    public Guid Id { get; set; }

    public Guid RecetaId { get; set; }

    public string? TipoRequerido { get; set; }

    public virtual Receta Receta { get; set; } = null!;
}
