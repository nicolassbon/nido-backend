using Nido.Application.Insights;

namespace Nido.Application.Alacena;

public sealed record GetStockMovementsQuery(
    Guid HogarId,
    string? Motivo,
    DateOnly? Desde,
    DateOnly? Hasta,
    string? Q,
    int Limit);

public sealed record StockMovementResult(
    Guid Id,
    Guid? ProductoId,
    string ProductoNombre,
    decimal Cantidad,
    string? UnidadMedida,
    string Motivo,
    DateTime FechaConsumo,
    Guid? UsuarioId);

public sealed class GetStockMovementsHandler
{
    private readonly IConsumoProductoRepository _repository;

    public GetStockMovementsHandler(IConsumoProductoRepository repository)
    {
        _repository = repository;
    }

    public async Task<IReadOnlyList<StockMovementResult>> Handle(GetStockMovementsQuery query, CancellationToken ct)
    {
        if (query.HogarId == Guid.Empty)
        {
            return Array.Empty<StockMovementResult>();
        }

        var limit = query.Limit <= 0 ? 100 : Math.Min(query.Limit, 200);
        var movements = await _repository.GetMovimientosAsync(
            query.HogarId,
            new ConsumoMovimientosFiltro(query.Motivo, query.Desde, query.Hasta, query.Q, limit),
            ct);

        return movements
            .Select(m => new StockMovementResult(
                m.Id,
                m.ProductoId,
                m.ProductoNombre,
                m.Cantidad,
                m.UnidadMedida,
                m.Motivo,
                m.FechaConsumo,
                m.UsuarioId))
            .ToList();
    }
}
