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

    [Fact]
    public async Task GetSugerenciasNidoAsync_OrdenaPorUrgenciaDeFrecuencia_ElMasProximoAAgotarQuedaPrimero()
    {
        var hogarId = Guid.NewGuid();
        var cafeId = Guid.NewGuid();
        var cafeStockId = Guid.NewGuid();
        var teId = Guid.NewGuid();
        var teStockId = Guid.NewGuid();

        var alacena = new FakeAlacenaRepository
        {
            Stock = [
                StockItem(cafeStockId, cafeId, "Café", cantidad: 1),
                StockItem(teStockId, teId, "Té", cantidad: 1),
            ]
        };
        var consumos = new FakeConsumoProductoRepository
        {
            Compras = [
                new ComprasPorProducto(cafeId, "Café", [DateTime.UtcNow.AddDays(-20), DateTime.UtcNow.AddDays(-10)]),
                new ComprasPorProducto(teId, "Té", [DateTime.UtcNow.AddDays(-25), DateTime.UtcNow.AddDays(-5)]),
            ],
            Consumos = [
                new ConsumoPorProducto(cafeId, "Café", 1m, 1, 0, 1, DateTime.UtcNow.AddDays(-3)),
                new ConsumoPorProducto(teId, "Té", 1m, 1, 0, 1, DateTime.UtcNow.AddDays(-3)),
            ],
        };
        var handler = new GetInsightsHogarHandler(alacena, consumos);

        var result = await handler.GetSugerenciasNidoAsync(hogarId, CancellationToken.None);

        Assert.Equal(2, result.Count);
        Assert.Equal("Café", result[0].ProductoNombre);
        Assert.Equal(0.6, result[0].Score, 4);
        Assert.Equal("Té", result[1].ProductoNombre);
        Assert.Equal(0.15, result[1].Score, 4);
    }

    [Fact]
    public async Task GetSugerenciasNidoAsync_StockBajoDelEnvaseAbierto_PuedeSuperarAUnoConMejorFrecuencia()
    {
        var hogarId = Guid.NewGuid();
        var mantecaId = Guid.NewGuid();
        var mantecaStockId = Guid.NewGuid();
        var harinaId = Guid.NewGuid();
        var harinaStockId = Guid.NewGuid();

        var alacena = new FakeAlacenaRepository
        {
            Stock = [
                StockItem(mantecaStockId, mantecaId, "Manteca", cantidad: 1, estaAbierto: true, porcentajeConsumido: 90, cantidadEnvases: 1),
                StockItem(harinaStockId, harinaId, "Harina", cantidad: 1),
            ]
        };
        var consumos = new FakeConsumoProductoRepository
        {
            Compras = [
                new ComprasPorProducto(mantecaId, "Manteca", [DateTime.UtcNow.AddDays(-25), DateTime.UtcNow.AddDays(-5)]),
                new ComprasPorProducto(harinaId, "Harina", [DateTime.UtcNow.AddDays(-30), DateTime.UtcNow.AddDays(-10)]),
            ],
            Consumos = [
                new ConsumoPorProducto(mantecaId, "Manteca", 1m, 1, 0, 1, DateTime.UtcNow.AddDays(-3)),
                new ConsumoPorProducto(harinaId, "Harina", 1m, 1, 0, 1, DateTime.UtcNow.AddDays(-3)),
            ],
        };
        var handler = new GetInsightsHogarHandler(alacena, consumos);

        var result = await handler.GetSugerenciasNidoAsync(hogarId, CancellationToken.None);

        Assert.Equal("Manteca", result[0].ProductoNombre);
        Assert.Equal(0.51, result[0].Score, 4);
        Assert.Equal("Harina", result[1].ProductoNombre);
        Assert.Equal(0.3, result[1].Score, 4);
    }

    [Fact]
    public async Task GetSugerenciasNidoAsync_CantidadEnvasesMayorAUno_NoSumaUrgenciaDeStock()
    {
        var hogarId = Guid.NewGuid();
        var productoId = Guid.NewGuid();
        var stockId = Guid.NewGuid();

        var alacena = new FakeAlacenaRepository
        {
            Stock = [StockItem(stockId, productoId, "Yogur", cantidad: 1, estaAbierto: true, porcentajeConsumido: 90, cantidadEnvases: 2)]
        };
        var consumos = new FakeConsumoProductoRepository
        {
            Compras = [new ComprasPorProducto(productoId, "Yogur", [DateTime.UtcNow.AddDays(-30), DateTime.UtcNow.AddDays(-10)])],
            Consumos = [new ConsumoPorProducto(productoId, "Yogur", 1m, 1, 0, 1, DateTime.UtcNow.AddDays(-3))],
        };
        var handler = new GetInsightsHogarHandler(alacena, consumos);

        var result = await handler.GetSugerenciasNidoAsync(hogarId, CancellationToken.None);

        var item = Assert.Single(result);
        Assert.Equal(0.3, item.Score, 4);
    }

    [Fact]
    public async Task GetSugerenciasNidoAsync_ResuelveIconoPorPalabraClaveDelNombre()
    {
        var hogarId = Guid.NewGuid();
        var productoId = Guid.NewGuid();
        var stockId = Guid.NewGuid();

        var alacena = new FakeAlacenaRepository
        {
            Stock = [StockItem(stockId, productoId, "Manteca", cantidad: 1)]
        };
        var consumos = new FakeConsumoProductoRepository
        {
            Compras = [new ComprasPorProducto(productoId, "Manteca", [DateTime.UtcNow.AddDays(-20), DateTime.UtcNow.AddDays(-10)])],
            Consumos = [new ConsumoPorProducto(productoId, "Manteca", 1m, 1, 0, 1, DateTime.UtcNow.AddDays(-3))],
        };
        var handler = new GetInsightsHogarHandler(alacena, consumos);

        var result = await handler.GetSugerenciasNidoAsync(hogarId, CancellationToken.None);

        var item = Assert.Single(result);
        Assert.Equal("milk", item.Icono);
    }

    [Fact]
    public async Task GetSugerenciasNidoAsync_ResuelveIconoPorCategoriaDelProducto()
    {
        var hogarId = Guid.NewGuid();
        var productoId = Guid.NewGuid();
        var stockId = Guid.NewGuid();

        var alacena = new FakeAlacenaRepository
        {
            Stock = [StockItem(stockId, productoId, "Producto Genérico", cantidad: 1, categoriaNombre: "Verduras")]
        };
        var consumos = new FakeConsumoProductoRepository
        {
            Compras = [new ComprasPorProducto(productoId, "Producto Genérico", [DateTime.UtcNow.AddDays(-20), DateTime.UtcNow.AddDays(-10)])],
            Consumos = [new ConsumoPorProducto(productoId, "Producto Genérico", 1m, 1, 0, 1, DateTime.UtcNow.AddDays(-3))],
        };
        var handler = new GetInsightsHogarHandler(alacena, consumos);

        var result = await handler.GetSugerenciasNidoAsync(hogarId, CancellationToken.None);

        var item = Assert.Single(result);
        Assert.Equal("carrot", item.Icono);
    }

    [Fact]
    public async Task GetSugerenciasNidoAsync_WhenNoHayEventoCocinado_NoIncluyeElItem()
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
            Compras = [new ComprasPorProducto(productoId, "Café", [DateTime.UtcNow.AddDays(-20), DateTime.UtcNow.AddDays(-10)])],
            Consumos = [new ConsumoPorProducto(productoId, "Café", 1m, 1, 0, 0, DateTime.UtcNow.AddDays(-3))],
        };
        var handler = new GetInsightsHogarHandler(alacena, consumos);

        var result = await handler.GetSugerenciasNidoAsync(hogarId, CancellationToken.None);

        Assert.Empty(result);
    }

    [Fact]
    public async Task GetSugerenciasNidoAsync_LimitaATop5()
    {
        var hogarId = Guid.NewGuid();
        var p1 = (Id: Guid.NewGuid(), StockId: Guid.NewGuid());
        var p2 = (Id: Guid.NewGuid(), StockId: Guid.NewGuid());
        var p3 = (Id: Guid.NewGuid(), StockId: Guid.NewGuid());
        var p4 = (Id: Guid.NewGuid(), StockId: Guid.NewGuid());
        var p5 = (Id: Guid.NewGuid(), StockId: Guid.NewGuid());
        var p6 = (Id: Guid.NewGuid(), StockId: Guid.NewGuid());

        var alacena = new FakeAlacenaRepository
        {
            Stock = [
                StockItem(p1.StockId, p1.Id, "P1", cantidad: 1),
                StockItem(p2.StockId, p2.Id, "P2", cantidad: 1),
                StockItem(p3.StockId, p3.Id, "P3", cantidad: 1),
                StockItem(p4.StockId, p4.Id, "P4", cantidad: 1),
                StockItem(p5.StockId, p5.Id, "P5", cantidad: 1),
                StockItem(p6.StockId, p6.Id, "P6", cantidad: 1),
            ]
        };
        var consumos = new FakeConsumoProductoRepository
        {
            Compras = [
                new ComprasPorProducto(p1.Id, "P1", [DateTime.UtcNow.AddDays(-22), DateTime.UtcNow.AddDays(-2)]),
                new ComprasPorProducto(p2.Id, "P2", [DateTime.UtcNow.AddDays(-24), DateTime.UtcNow.AddDays(-4)]),
                new ComprasPorProducto(p3.Id, "P3", [DateTime.UtcNow.AddDays(-26), DateTime.UtcNow.AddDays(-6)]),
                new ComprasPorProducto(p4.Id, "P4", [DateTime.UtcNow.AddDays(-28), DateTime.UtcNow.AddDays(-8)]),
                new ComprasPorProducto(p5.Id, "P5", [DateTime.UtcNow.AddDays(-30), DateTime.UtcNow.AddDays(-10)]),
                new ComprasPorProducto(p6.Id, "P6", [DateTime.UtcNow.AddDays(-32), DateTime.UtcNow.AddDays(-12)]),
            ],
            Consumos = [
                new ConsumoPorProducto(p1.Id, "P1", 1m, 1, 0, 1, DateTime.UtcNow.AddDays(-3)),
                new ConsumoPorProducto(p2.Id, "P2", 1m, 1, 0, 1, DateTime.UtcNow.AddDays(-3)),
                new ConsumoPorProducto(p3.Id, "P3", 1m, 1, 0, 1, DateTime.UtcNow.AddDays(-3)),
                new ConsumoPorProducto(p4.Id, "P4", 1m, 1, 0, 1, DateTime.UtcNow.AddDays(-3)),
                new ConsumoPorProducto(p5.Id, "P5", 1m, 1, 0, 1, DateTime.UtcNow.AddDays(-3)),
                new ConsumoPorProducto(p6.Id, "P6", 1m, 1, 0, 1, DateTime.UtcNow.AddDays(-3)),
            ],
        };
        var handler = new GetInsightsHogarHandler(alacena, consumos);

        var result = await handler.GetSugerenciasNidoAsync(hogarId, CancellationToken.None);

        Assert.Equal(5, result.Count);
        Assert.Equal(["P6", "P5", "P4", "P3", "P2"], result.Select(x => x.ProductoNombre));
        Assert.DoesNotContain(result, x => x.ProductoNombre == "P1");
    }

    private static StockItemResult StockItem(
        Guid id, Guid productoId, string nombre, decimal cantidad,
        bool estaAbierto = false, decimal porcentajeConsumido = 0, int cantidadEnvases = 1, string? categoriaNombre = null)
        => new(id, productoId, nombre, null, null, categoriaNombre, "Alacena", cantidad, "unidad", null, estaAbierto, porcentajeConsumido, cantidadEnvases);

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
