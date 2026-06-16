namespace Nido.Api.Contracts.Estadisticas;

public sealed record EstadisticasConsumoResponse(
    int Total,
    int TotalAbiertos,
    int TotalPorVencer,
    int TotalParaReponer,
    IReadOnlyList<ProductoConsumoResponse> Items
);

public sealed record ProductoConsumoResponse(
    Guid     StockId,
    Guid     ProductoId,
    string   Nombre,
    string?  CategoriaNombre,
    string?  Ubicacion,
    decimal  CantidadActual,
    string?  UnidadMedida,
    bool     EstaAbierto,
    decimal  PorcentajeConsumido,
    string?  FechaVencimiento,
    int?     DiasHastaVencimiento,
    decimal? TasaConsumoDiariaEstimada,
    int?     DiasHastaAgotamientoEstimado,
    string   PrioridadReposicion
);
