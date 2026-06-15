using Nido.Application.Productos;
using Nido.Domain.StockHogar;

namespace Nido.Application.Tests.Productos;

public sealed class CreateStockHomeHandlerTests
{
    [Fact]
    public async Task Handle_WithExactIsoDate_PersistsDateAndReturnsSameFormat()
    {
        var productoId = Guid.NewGuid();
        var productoRepository = new FakeProductoRepository
        {
            CreatedProduct = new GetProductByNameResult(productoId, "Yerba", null, null)
        };
        var stockRepository = new FakeStockHogarRepository();
        var handler = new CreateStockHomeHandler(stockRepository, productoRepository);

        var result = await handler.Handle(
            new CreateStockHomeCommand(
                "Yerba",
                null,
                "Alacena",
                1,
                "kg",
                "2026-12-01",
                Guid.NewGuid(),
                Guid.NewGuid()),
            CancellationToken.None);

        Assert.Equal("2026-12-01", result.FechaVencimiento);
        Assert.NotNull(stockRepository.SavedStock);
        Assert.Equal(new DateTime(2026, 12, 1), stockRepository.SavedStock!.FechaVencimiento);
    }

    [Theory]
    [InlineData("01/12/2026")]
    [InlineData("2026-12-1")]
    [InlineData("2026-12-01 ")]
    public async Task Handle_WithNonExactDateFormat_ThrowsArgumentException(string fechaVencimiento)
    {
        var handler = new CreateStockHomeHandler(
            new FakeStockHogarRepository(),
            new FakeProductoRepository
            {
                CreatedProduct = new GetProductByNameResult(Guid.NewGuid(), "Yerba", null, null)
            });

        var exception = await Assert.ThrowsAsync<ArgumentException>(() =>
            handler.Handle(
                new CreateStockHomeCommand(
                    "Yerba",
                    null,
                    "Alacena",
                    1,
                    "kg",
                    fechaVencimiento,
                    Guid.NewGuid(),
                    Guid.NewGuid()),
                CancellationToken.None));

        Assert.Equal("La fecha de vencimiento debe tener formato yyyy-MM-dd.", exception.Message);
    }

    [Fact]
    public async Task Handle_AlwaysCreatesADedicatedManualProduct()
    {
        var createdProduct = new GetProductByNameResult(Guid.NewGuid(), "Yerba", Guid.NewGuid(), null);
        var productoRepository = new FakeProductoRepository
        {
            ExistingProduct = new GetProductByNameResult(Guid.NewGuid(), "Yerba", null, null),
            CreatedProduct = createdProduct
        };
        var handler = new CreateStockHomeHandler(new FakeStockHogarRepository(), productoRepository);

        var result = await handler.Handle(
            new CreateStockHomeCommand(
                "Yerba",
                createdProduct.CategoriaId,
                "Alacena",
                1,
                "kg",
                null,
                Guid.NewGuid(),
                Guid.NewGuid()),
            CancellationToken.None);

        Assert.Equal(1, productoRepository.CreateCalls);
        Assert.Equal(createdProduct.Id, result.ProductoId);
    }

    private sealed class FakeStockHogarRepository : IStockHogarRepository
    {
        public StockHogar? SavedStock { get; private set; }

        public Task SaveAsync(StockHogar stockHogar, CancellationToken cancellationToken)
        {
            SavedStock = stockHogar;
            return Task.CompletedTask;
        }
    }

    private sealed class FakeProductoRepository : IProductoRepository
    {
        public GetProductByNameResult? ExistingProduct { get; set; }
        public GetProductByNameResult CreatedProduct { get; set; } = new(Guid.NewGuid(), "Yerba", null, null);
        public int CreateCalls { get; private set; }

        public Task<GetProductByBarcodeResult?> GetByBarcodeAsync(string barcode, CancellationToken ct)
            => throw new NotSupportedException();

        public Task<GetProductByNameResult?> GetByNameAsync(string nombre, CancellationToken ct)
            => Task.FromResult(ExistingProduct);

        public Task<GetProductByNameResult> CreateAsync(string nombre, Guid? categoriaId, CancellationToken ct)
        {
            CreateCalls++;
            return Task.FromResult(CreatedProduct);
        }
    }
}
