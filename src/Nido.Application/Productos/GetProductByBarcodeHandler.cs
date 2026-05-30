namespace Nido.Application.Productos;

public sealed class GetProductByBarcodeHandler
{
    private readonly IProductoRepository _repository;

    public GetProductByBarcodeHandler(IProductoRepository repository)
    {
        _repository = repository;
    }

    public async Task<GetProductByBarcodeResult?> Handle(GetProductByBarcodeQuery query, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(query.CodigoBarras))
        {
            throw new ArgumentException("El código de barras es requerido.");
        }

        return await _repository.GetByBarcodeAsync(query.CodigoBarras.Trim(), ct);
    }
}
