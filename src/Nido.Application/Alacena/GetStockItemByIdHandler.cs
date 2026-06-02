using Nido.Application.Alacena.Exceptions;

namespace Nido.Application.Alacena;

public sealed class GetStockItemByIdHandler
{
    private readonly IAlacenaRepository _repository;

    public GetStockItemByIdHandler(IAlacenaRepository repository)
    {
        _repository = repository;
    }

    public async Task<StockItemResult?> Handle(GetStockItemByIdQuery query, CancellationToken ct)
    {
        if (query.Id == Guid.Empty)
        {
            throw new MissingStockItemFieldException("producto");
        }

        if (query.HogarId == Guid.Empty)
        {
            throw new MissingStockItemFieldException("hogar");
        }

        return await _repository.GetByIdAsync(query.Id, query.HogarId, ct);
    }
}
