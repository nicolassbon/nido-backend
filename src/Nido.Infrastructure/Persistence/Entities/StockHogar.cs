using System;
using System.Collections.Generic;

namespace Nido.Infrastructure.Persistence.Entities;

public partial class StockHogar
{
    public Guid Id { get; set; }

    public Guid HogarId { get; set; }

    public Guid ProductoId { get; set; }

    public Guid CargadoPor { get; set; }

    public decimal? CantidadActual { get; set; }

    public string? UnidadMedida { get; set; }

    public DateOnly? FechaVencimiento { get; set; }

    public DateTime CreatedAt { get; set; }

    public Guid UpdatedBy { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public virtual Usuario CargadoPorNavigation { get; set; } = null!;

    public virtual Hogare Hogar { get; set; } = null!;

    public virtual Producto Producto { get; set; } = null!;

    public virtual Usuario UpdatedByNavigation { get; set; } = null!;

    // Added post-scaffold (not in original schema)
    public string Ubicacion { get; set; } = null!;
    public bool EstaAbierto { get; set; }
    public decimal PorcentajeConsumido { get; set; }

    /// <summary>
    /// Cantidad de envases idénticos del producto. Default = 1.
    /// Ej: cantidad_envases=2 + cantidad_actual=100 + unidad_medida='gr' →
    /// "2 paquetes de 100g cada uno" (total disponible: 200g menos lo consumido del abierto).
    /// </summary>
    public int CantidadEnvases { get; set; } = 1;
}
