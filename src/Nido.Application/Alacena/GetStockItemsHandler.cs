using Nido.Application.Alacena.Exceptions;

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
            throw new MissingStockItemFieldException("hogar");
        }

        return await _repository.GetByHogarAsync(query.HogarId, ct);
    }
}
