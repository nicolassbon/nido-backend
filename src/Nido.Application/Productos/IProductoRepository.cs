namespace Nido.Application.Productos;

public interface IProductoRepository
{
    Task<GetProductByBarcodeResult?> GetByBarcodeAsync(string barcode, CancellationToken ct);
}
