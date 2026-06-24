namespace Nido.Application.Estadisticas;

public sealed record StockSnapshot(
    Guid     StockId,
    Guid     ProductoId,
    string   Nombre,
    string?  CategoriaNombre,
    string?  Ubicacion,
    decimal  CantidadActual,
    string?  UnidadMedida,
    bool     EstaAbierto,
    decimal  PorcentajeConsumido,
    DateOnly? FechaVencimiento,
    DateTime CreatedAt
);

public interface IEstadisticasRepository
{
    /// <summary>
    /// Devuelve todos los items del stock activo de un hogar con la info necesaria
    /// para calcular estadísticas (sin computar nada, eso lo hace el handler).
    /// </summary>
    Task<IReadOnlyList<StockSnapshot>> GetStockSnapshotAsync(Guid hogarId, CancellationToken ct);
}
