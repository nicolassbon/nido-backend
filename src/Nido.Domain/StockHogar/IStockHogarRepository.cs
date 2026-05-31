namespace Nido.Domain.StockHogar;

public interface IStockHogarRepository
{
    Task SaveAsync(
        StockHogar stockHogar,
        CancellationToken cancellationToken);
}