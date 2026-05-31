using Nido.Domain.StockHogar;

namespace Nido.Application.Productos;


public interface IStockHogarRepository
{
    Task SaveAsync(
        StockHogar stockHogar,
        CancellationToken cancellationToken);
}