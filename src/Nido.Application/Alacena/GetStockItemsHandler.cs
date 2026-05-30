namespace Nido.Application.Alacena;

public sealed class GetStockItemsHandler
{
    private readonly IAlacenaRepository _repository;

    public GetStockItemsHandler(IAlacenaRepository repository)
    {
        _repository = repository;
    }

    public async Task<IReadOnlyList<StockItemResult>> Handle(GetStockItemsQuery query, CancellationToken ct)
    {
        if (query.HogarId == Guid.Empty)
        {
            throw new ArgumentException("El hogar es requerido.");
        }

        return await _repository.GetByHogarAsync(query.HogarId, ct);
    }
}
