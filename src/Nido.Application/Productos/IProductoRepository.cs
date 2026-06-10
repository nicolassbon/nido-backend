namespace Nido.Application.Productos;

public interface IProductoRepository
{
    Task<GetProductByBarcodeResult?> GetByBarcodeAsync(string barcode, CancellationToken ct);

    Task<GetProductByNameResult?> GetByNameAsync(string nombre, CancellationToken ct);

    Task<IEnumerable<SearchProductosResult>> SearchByNombreAsync(string query, CancellationToken ct);
}
