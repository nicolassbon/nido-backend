namespace Nido.Application.Estadisticas;

/// <summary>
/// Resultado de las estadísticas de consumo del hogar.
/// </summary>
public sealed record EstadisticasConsumoResult(
    int Total,
    int TotalAbiertos,
    int TotalPorVencer,
    int TotalParaReponer,
    IReadOnlyList<ProductoConsumoItem> Items
);

public sealed record ProductoConsumoItem(
    Guid     StockId,
    Guid     ProductoId,
    string   Nombre,
    string?  CategoriaNombre,
    string?  Ubicacion,
    decimal  CantidadActual,
    string?  UnidadMedida,
    bool     EstaAbierto,
    decimal  PorcentajeConsumido,
    string?  FechaVencimiento,                 // ISO yyyy-MM-dd o null
    int?     DiasHastaVencimiento,             // negativo si ya vencido
    decimal? TasaConsumoDiariaEstimada,        // cantidad/dia
    int?     DiasHastaAgotamientoEstimado,     // null si tasa es 0 o desconocida
    string   PrioridadReposicion              // 'urgente' | 'pronto' | 'normal' | 'ninguna'
);
