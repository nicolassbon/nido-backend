using System;
using System.Collections.Generic;

namespace Nido.Infrastructure.Persistence.Entities;

public partial class IngredientesRecetum
{
    public Guid Id { get; set; }

    public Guid RecetaId { get; set; }

    public string NombreIngrediente { get; set; } = null!;

    public Guid? ProductoId { get; set; }

    public decimal? Cantidad { get; set; }

    public string? Unidad { get; set; }

    public virtual Producto? Producto { get; set; }

    public virtual Receta Receta { get; set; } = null!;
}
