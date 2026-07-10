using Nido.Application.Alacena;
using Nido.Application.Insights;

namespace Nido.Application.Tests.Insights;

public sealed class GetInsightsHogarHandlerTests
{
    [Fact]
    public async Task GetComprarProntoAsync_WhenFrecuenciaIndicaAgotamientoCercano_IncluyeElItem()
    {
        var hogarId = Guid.NewGuid();
        var productoId = Guid.NewGuid();
        var stockId = Guid.NewGuid();

        var alacena = new FakeAlacenaRepository
        {
            Stock = [StockItem(stockId, productoId, "Café", cantidad: 1)]
        };
        var consumos = new FakeConsumoProductoRepository
        {
            Compras = [new ComprasPorProducto(productoId, "Café", [
                DateTime.UtcNow.AddDays(-20),
                DateTime.UtcNow.AddDays(-8),
            ])],
            Consumos = [new ConsumoPorProducto(productoId, "Café", 1m, 1, 0, 1, DateTime.UtcNow.AddDays(-3))],
        };
        var handler = new GetInsightsHogarHandler(alacena, consumos);

        var result = await handler.GetComprarProntoAsync(hogarId, CancellationToken.None);

        var item = Assert.Single(result);
        Assert.Equal(stockId, item.StockHogarId);
        Assert.Equal("Café", item.ProductoNombre);
        Assert.Equal(12, item.FrecuenciaCompraDias);
        Assert.Equal(4, item.DiasParaAgotar);
    }

    [Fact]
    public async Task GetComprarProntoAsync_WhenNoHayEventoCocinado_NoIncluyeElItem()
    {
        var hogarId = Guid.NewGuid();
        var productoId = Guid.NewGuid();
        var stockId = Guid.NewGuid();

        var alacena = new FakeAlacenaRepository
        {
            Stock = [StockItem(stockId, productoId, "Café", cantidad: 1)]
        };
        var consumos = new FakeConsumoProductoRepository
        {
            Compras = [new ComprasPorProducto(productoId, "Café", [
                DateTime.UtcNow.AddDays(-20),
                DateTime.UtcNow.AddDays(-8),
            ])],
            Consumos = [new ConsumoPorProducto(productoId, "Café", 1m, 1, 0, 0, DateTime.UtcNow.AddDays(-3))],
        };
        var handler = new GetInsightsHogarHandler(alacena, consumos);

        var result = await handler.GetComprarProntoAsync(hogarId, CancellationToken.None);

        Assert.Empty(result);
    }

    [Fact]
    public async Task GetComprarProntoAsync_WhenSoloHayUnaCompra_NoIncluyeElItem()
    {
        var hogarId = Guid.NewGuid();
        var productoId = Guid.NewGuid();
        var stockId = Guid.NewGuid();

        var alacena = new FakeAlacenaRepository
        {
            Stock = [StockItem(stockId, productoId, "Café", cantidad: 1)]
        };
        var consumos = new FakeConsumoProductoRepository
        {
            Compras = [new ComprasPorProducto(productoId, "Café", [DateTime.UtcNow.AddDays(-8)])],
            Consumos = [new ConsumoPorProducto(productoId, "Café", 1m, 1, 0, 1, DateTime.UtcNow.AddDays(-3))],
        };
        var handler = new GetInsightsHogarHandler(alacena, consumos);

        var result = await handler.GetComprarProntoAsync(hogarId, CancellationToken.None);

        Assert.Empty(result);
    }

    [Fact]
    public async Task GetComprarProntoAsync_WhenStockEnCero_NoIncluyeElItem()
    {
        var hogarId = Guid.NewGuid();
        var productoId = Guid.NewGuid();
        var stockId = Guid.NewGuid();

        var alacena = new FakeAlacenaRepository
        {
            Stock = [StockItem(stockId, productoId, "Café", cantidad: 0)]
        };
        var consumos = new FakeConsumoProductoRepository
        {
            Compras = [new ComprasPorProducto(productoId, "Café", [
                DateTime.UtcNow.AddDays(-20),
                DateTime.UtcNow.AddDays(-8),
            ])],
            Consumos = [new ConsumoPorProducto(productoId, "Café", 1m, 1, 0, 1, DateTime.UtcNow.AddDays(-3))],
        };
        var handler = new GetInsightsHogarHandler(alacena, consumos);

        var result = await handler.GetComprarProntoAsync(hogarId, CancellationToken.None);

        Assert.Empty(result);
    }

    [Fact]
    public async Task GetComprarProntoAsync_WhenFaltanMuchosDiasParaAgotarse_NoIncluyeElItem()
    {
        var hogarId = Guid.NewGuid();
        var productoId = Guid.NewGuid();
        var stockId = Guid.NewGuid();

        var alacena = new FakeAlacenaRepository
        {
            Stock = [StockItem(stockId, productoId, "Café", cantidad: 1)]
        };
        var consumos = new FakeConsumoProductoRepository
        {
            Compras = [new ComprasPorProducto(productoId, "Café", [
                DateTime.UtcNow.AddDays(-31),
                DateTime.UtcNow.AddDays(-1),
            ])],
            Consumos = [new ConsumoPorProducto(productoId, "Café", 1m, 1, 0, 1, DateTime.UtcNow.AddDays(-3))],
        };
        var handler = new GetInsightsHogarHandler(alacena, consumos);

        var result = await handler.GetComprarProntoAsync(hogarId, CancellationToken.None);

        Assert.Empty(result);
    }

    private static StockItemResult StockItem(Guid id, Guid productoId, string nombre, decimal cantidad)
        => new(id, productoId, nombre, null, null, null, "Alacena", cantidad, "unidad", null, false, 0, 1);

    private sealed class FakeAlacenaRepository : IAlacenaRepository
    {
        public IReadOnlyList<StockItemResult> Stock { get; set; } = [];

        public Task<IReadOnlyList<StockItemResult>> GetByHogarAsync(Guid hogarId, CancellationToken ct)
            => Task.FromResult(Stock);

        public Task<StockItemResult?> GetByIdAsync(Guid id, Guid hogarId, CancellationToken ct)
            => Task.FromResult<StockItemResult?>(null);

        public Task<StockItemResult> CreateAsync(CreateStockItemRequestModel request, CancellationToken ct)
            => throw new NotSupportedException();

        public Task<StockItemResult?> UpdateAsync(UpdateStockItemRequestModel request, CancellationToken ct)
            => Task.FromResult<StockItemResult?>(null);

        public Task<bool> DeleteAsync(Guid id, Guid hogarId, CancellationToken ct)
            => Task.FromResult(false);
    }

    private sealed class FakeConsumoProductoRepository : IConsumoProductoRepository
    {
        public IReadOnlyList<ConsumoPorProducto> Consumos { get; set; } = [];
        public IReadOnlyList<ComprasPorProducto> Compras { get; set; } = [];

        public Task RegistrarAsync(RegistrarConsumoInput input, CancellationToken ct)
            => Task.CompletedTask;

        public Task<IReadOnlyList<ConsumoPorProducto>> GetConsumosPorProductoAsync(Guid hogarId, int diasAtras, CancellationToken ct)
            => Task.FromResult(Consumos);

        public Task<IReadOnlyList<ConsumoMovimiento>> GetMovimientosAsync(Guid hogarId, ConsumoMovimientosFiltro filtro, CancellationToken ct)
            => Task.FromResult<IReadOnlyList<ConsumoMovimiento>>([]);

        public Task<IReadOnlyList<ComprasPorProducto>> GetComprasPorProductoAsync(Guid hogarId, int diasAtras, CancellationToken ct)
            => Task.FromResult(Compras);

        public Task<IReadOnlyDictionary<DayOfWeek, int>> GetCocinadasPorDiaSemanaAsync(Guid hogarId, int diasAtras, CancellationToken ct)
            => Task.FromResult<IReadOnlyDictionary<DayOfWeek, int>>(new Dictionary<DayOfWeek, int>());

        public Task<IReadOnlyList<RecetaTopItem>> GetRecetasTopAsync(Guid hogarId, int diasAtras, int topN, CancellationToken ct)
            => Task.FromResult<IReadOnlyList<RecetaTopItem>>([]);

        public Task<IReadOnlyList<EnvaseZombieRaw>> GetEnvasesAbiertosLargoAsync(Guid hogarId, int diasMinAbierto, CancellationToken ct)
            => Task.FromResult<IReadOnlyList<EnvaseZombieRaw>>([]);

        public Task<IReadOnlyList<DateTime>> GetFechasComprasHogarAsync(Guid hogarId, int diasAtras, CancellationToken ct)
            => Task.FromResult<IReadOnlyList<DateTime>>([]);
    }
}
