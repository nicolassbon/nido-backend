namespace Nido.Application.Productos;

public interface IProductoRepository
{
    Task<GetProductByBarcodeResult?> GetByBarcodeAsync(string barcode, CancellationToken ct);

    Task<GetProductByNameResult?> GetByNameAsync(string nombre, CancellationToken ct);

    Task<GetProductByNameResult> CreateAsync(string nombre, Guid? categoriaId, CancellationToken ct);
}
