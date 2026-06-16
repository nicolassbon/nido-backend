using System;

namespace Nido.Infrastructure.Persistence.Entities;

/// <summary>
/// Registra cada salida de stock de un producto del hogar.
/// Alimenta el motor de recomendaciones inteligentes (tasa de consumo,
/// predicción de agotamiento, detección de desperdicio).
/// </summary>
public class ConsumoProducto
{
    public Guid Id { get; set; }

    public Guid HogarId { get; set; }

    public Guid? ProductoId { get; set; }

    /// <summary>Snapshot del nombre por si el producto se borra del catálogo.</summary>
    public string ProductoNombre { get; set; } = null!;

    public Guid? CategoriaId { get; set; }

    public decimal Cantidad { get; set; }

    public string? UnidadMedida { get; set; }

    public DateTime FechaConsumo { get; set; }

    /// <summary>Consumido | Descartado | Cocinado | Vencido | Ajuste</summary>
    public string Motivo { get; set; } = null!;

    public Guid? UsuarioId { get; set; }

    public virtual Hogare? Hogar { get; set; }
    public virtual Producto? Producto { get; set; }
}
