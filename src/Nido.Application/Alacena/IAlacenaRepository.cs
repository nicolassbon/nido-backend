namespace Nido.Application.Alacena;

public interface IAlacenaRepository
{
    Task<IReadOnlyList<StockItemResult>> GetByHogarAsync(Guid hogarId, CancellationToken ct);
    Task<StockItemResult> CreateAsync(CreateStockItemRequestModel request, CancellationToken ct);
    Task<StockItemResult?> UpdateAsync(UpdateStockItemRequestModel request, CancellationToken ct);
    Task<bool> DeleteAsync(Guid id, CancellationToken ct);
}
