using Nido.Application.Productos;
using Nido.Application.Productos.Exceptions;

namespace Nido.Application.Tests.Productos;

public sealed class GetProductByBarcodeHandlerTests
{
    [Fact]
    public async Task Handle_CuandoExisteProducto_RetornaResultado()
    {
        var expected = new GetProductByBarcodeResult(
            Guid.NewGuid(),
            "Leche",
            "7791234567890",
            "https://img.test/leche.png",
            "Lácteos",
            7);

        var repo = new FakeProductoRepository { Producto = expected };
        var handler = new GetProductByBarcodeHandler(repo);

        var result = await handler.Handle(new GetProductByBarcodeQuery(expected.CodigoBarras!), CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(expected.Id, result!.Id);
        Assert.Equal(expected.Nombre, result.Nombre);
        Assert.Equal(expected.CategoriaNombre, result.CategoriaNombre);
    }

    [Fact]
    public async Task Handle_CuandoNoExisteProducto_RetornaNull()
    {
        var repo = new FakeProductoRepository { Producto = null };
        var handler = new GetProductByBarcodeHandler(repo);

        var result = await handler.Handle(new GetProductByBarcodeQuery("0000000"), CancellationToken.None);

        Assert.Null(result);
    }

    [Fact]
    public async Task Handle_CuandoCodigoEsVacio_LanzaMissingProductField()
    {
        var repo = new FakeProductoRepository();
        var handler = new GetProductByBarcodeHandler(repo);

        await Assert.ThrowsAsync<MissingProductFieldException>(() =>
            handler.Handle(new GetProductByBarcodeQuery("  "), CancellationToken.None));
    }

    private sealed class FakeProductoRepository : IProductoRepository
    {
        public GetProductByBarcodeResult? Producto { get; set; }

        public Task<GetProductByBarcodeResult?> GetByBarcodeAsync(string barcode, Guid? hogarId, CancellationToken ct)
            => Task.FromResult(Producto);

        public Task<GetProductByNameResult?> GetByNameAsync(string nombre, CancellationToken ct)
            => Task.FromResult<GetProductByNameResult?>(null);

        public Task<IEnumerable<SearchProductosResult>> SearchByNombreAsync(string query, CancellationToken ct)
            => Task.FromResult(Enumerable.Empty<SearchProductosResult>());

        public Task<GetProductByNameResult> CreateAsync(string nombre, Guid? categoriaId, CancellationToken ct,
            decimal? calorias = null, decimal? proteinas = null, decimal? carbohidratos = null, decimal? grasas = null)
            => throw new NotSupportedException("CreateAsync should not be called by GetProductByBarcodeHandler tests.");
    }
}
