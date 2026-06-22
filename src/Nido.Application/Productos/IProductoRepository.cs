namespace Nido.Application.Productos;

public interface IProductoRepository
{
    Task<GetProductByBarcodeResult?> GetByBarcodeAsync(string barcode, Guid? hogarId, CancellationToken ct);

    Task<GetProductByNameResult?> GetByNameAsync(string nombre, CancellationToken ct);

    Task<IEnumerable<SearchProductosResult>> SearchByNombreAsync(string query, CancellationToken ct);

    /// <summary>
    /// Crea un producto nuevo en el catálogo. Se usa cuando el usuario carga
    /// manualmente un producto que aún no existe en la base global.
    /// </summary>
    Task<GetProductByNameResult> CreateAsync(
        string nombre,
        Guid? categoriaId,
        CancellationToken ct,
        decimal? calorias = null,
        decimal? proteinas = null,
        decimal? carbohidratos = null,
        decimal? grasas = null);
}
