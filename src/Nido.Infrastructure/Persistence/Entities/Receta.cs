using System;
using System.Collections.Generic;

namespace Nido.Infrastructure.Persistence.Entities;

public partial class Receta
{
    public Guid Id { get; set; }

    public string Nombre { get; set; } = null!;

    public string? Descripcion { get; set; }

    public int? TiempoCoccionMin { get; set; }

    public string? Dificultad { get; set; }

    public int? Porciones { get; set; }

    public string? FuenteId { get; set; }

    public virtual ICollection<InfoNutricionalRecetum> InfoNutricionalReceta { get; set; } = new List<InfoNutricionalRecetum>();

    public virtual ICollection<IngredientesRecetum> IngredientesReceta { get; set; } = new List<IngredientesRecetum>();

    public virtual ICollection<RecetaElectrodomestico> RecetaElectrodomesticos { get; set; } = new List<RecetaElectrodomestico>();

    public virtual ICollection<RecetasCocinada> RecetasCocinada { get; set; } = new List<RecetasCocinada>();

    public virtual ICollection<ReseniasRecetum> ReseniasReceta { get; set; } = new List<ReseniasRecetum>();
}
