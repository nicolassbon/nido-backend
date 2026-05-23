using System;
using System.Collections.Generic;

namespace Nido.Infrastructure.Persistence.Entities;

public partial class Producto
{
    public Guid Id { get; set; }

    public string Nombre { get; set; } = null!;

    public string? CodigoBarras { get; set; }

    public string? ImagenUrl { get; set; }

    public Guid? CategoriaId { get; set; }

    public virtual CategoriasProducto? Categoria { get; set; }

    public virtual ICollection<InfoNutricionalProducto> InfoNutricionalProductos { get; set; } = new List<InfoNutricionalProducto>();

    public virtual ICollection<IngredientesRecetum> IngredientesReceta { get; set; } = new List<IngredientesRecetum>();

    public virtual ICollection<ListaCompra> ListaCompras { get; set; } = new List<ListaCompra>();

    public virtual ICollection<StockHogar> StockHogars { get; set; } = new List<StockHogar>();
}
