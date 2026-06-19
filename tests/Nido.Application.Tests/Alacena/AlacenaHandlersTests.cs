using Nido.Application.Alacena;
using Nido.Application.Alacena.Exceptions;
using Nido.Application.Insights;

namespace Nido.Application.Tests.Alacena;

public sealed class AlacenaHandlersTests
{
    [Fact]
    public async Task GetByHogar_ReturnsItemsFromRepository()
    {
        var hogarId = Guid.NewGuid();
        var repo = new FakeAlacenaRepository
        {
            Items =
            [
                new StockItemResult(Guid.NewGuid(), Guid.NewGuid(), "Arroz", null, "779", null, "Alacena", 1, "unidad", null, false, 0, 1),
            ]
        };

        var handler = new GetStockItemsHandler(repo);

        var result = await handler.Handle(new GetStockItemsQuery(hogarId), CancellationToken.None);

        Assert.Single(result);
        Assert.Equal("Arroz", result[0].Nombre);
    }

    [Fact]
    public async Task Create_WithInvalidDate_ThrowsInvalidStockItemDate()
    {
        var repo = new FakeAlacenaRepository();
        var handler = new CreateStockItemHandler(repo);

        await Assert.ThrowsAsync<InvalidStockItemDateException>(() =>
            handler.Handle(
                new CreateStockItemCommand(Guid.NewGuid(), Guid.NewGuid(), "Arroz", null, null, "Alacena", 1, "g", "no-fecha", false, 0),
                CancellationToken.None));
    }

    [Fact]
    public async Task Update_WhenNotExists_ReturnsNull()
    {
        var repo = new FakeAlacenaRepository { UpdatedResult = null };
        var handler = new UpdateStockItemHandler(repo);

        var result = await handler.Handle(
            new UpdateStockItemCommand(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), null, null, null, null, null, null, null),
            CancellationToken.None);

        Assert.Null(result);
    }

    [Fact]
    public async Task Delete_WhenExists_ReturnsTrue()
    {
        var hogarId = Guid.NewGuid();
        var usuarioId = Guid.NewGuid();
        var stockId = Guid.NewGuid();
        var productoId = Guid.NewGuid();
        var repo = new FakeAlacenaRepository
        {
            DeleteResult = true,
            Items =
            [
                new StockItemResult(stockId, productoId, "Arroz", null, "779", null, "Alacena", 2.5m, "kg", null, false, 0, 1),
            ]
        };
        var consumos = new FakeConsumoRepository();
        var handler = new DeleteStockItemHandler(repo, consumos);

        var result = await handler.Handle(new DeleteStockItemCommand(stockId, hogarId, usuarioId), CancellationToken.None);

        Assert.True(result);
        Assert.Equal(stockId, repo.LastGetByIdId);
        Assert.Equal(hogarId, repo.LastGetByIdHogarId);
        Assert.Equal(stockId, repo.LastDeleteId);
        Assert.Equal(hogarId, repo.LastDeleteHogarId);

        var consumo = Assert.Single(consumos.Registros);
        Assert.Equal(hogarId, consumo.HogarId);
        Assert.Equal(productoId, consumo.ProductoId);
        Assert.Equal("Arroz", consumo.ProductoNombre);
        Assert.Equal(2.5m, consumo.Cantidad);
        Assert.Equal("kg", consumo.UnidadMedida);
        Assert.Equal(ConsumoMotivos.Consumido, consumo.Motivo);
        Assert.Equal(usuarioId, consumo.UsuarioId);
    }

    [Fact]
    public async Task Delete_WhenExpiredItem_RegistersVencidoConsumo()
    {
        var hogarId = Guid.NewGuid();
        var usuarioId = Guid.NewGuid();
        var stockId = Guid.NewGuid();
        var productoId = Guid.NewGuid();
        var repo = new FakeAlacenaRepository
        {
            DeleteResult = true,
            Items =
            [
                new StockItemResult(stockId, productoId, "Leche", null, null, null, "Heladera", 1m, "lt", DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-1)).ToString("yyyy-MM-dd"), false, 0, 1),
            ]
        };
        var consumos = new FakeConsumoRepository();
        var handler = new DeleteStockItemHandler(repo, consumos);

        var result = await handler.Handle(new DeleteStockItemCommand(stockId, hogarId, usuarioId), CancellationToken.None);

        Assert.True(result);
        var consumo = Assert.Single(consumos.Registros);
        Assert.Equal(ConsumoMotivos.Vencido, consumo.Motivo);
    }

    [Fact]
    public async Task Delete_WhenExpiredItemWithConsumidoMotivo_RegistersConsumidoConsumo()
    {
        var hogarId = Guid.NewGuid();
        var usuarioId = Guid.NewGuid();
        var stockId = Guid.NewGuid();
        var productoId = Guid.NewGuid();
        var repo = new FakeAlacenaRepository
        {
            DeleteResult = true,
            Items =
            [
                new StockItemResult(stockId, productoId, "Leche", null, null, null, "Heladera", 1m, "lt", DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-1)).ToString("yyyy-MM-dd"), false, 0, 1),
            ]
        };
        var consumos = new FakeConsumoRepository();
        var handler = new DeleteStockItemHandler(repo, consumos);

        var result = await handler.Handle(
            new DeleteStockItemCommand(stockId, hogarId, usuarioId, ConsumoMotivos.Consumido),
            CancellationToken.None);

        Assert.True(result);
        var consumo = Assert.Single(consumos.Registros);
        Assert.Equal(ConsumoMotivos.Consumido, consumo.Motivo);
    }

    [Fact]
    public async Task Delete_WhenNonExpiredItemWithVencidoMotivo_RegistersVencidoConsumo()
    {
        var hogarId = Guid.NewGuid();
        var usuarioId = Guid.NewGuid();
        var stockId = Guid.NewGuid();
        var productoId = Guid.NewGuid();
        var repo = new FakeAlacenaRepository
        {
            DeleteResult = true,
            Items =
            [
                new StockItemResult(stockId, productoId, "Arroz", null, null, null, "Alacena", 1m, "kg", null, false, 0, 1),
            ]
        };
        var consumos = new FakeConsumoRepository();
        var handler = new DeleteStockItemHandler(repo, consumos);

        var result = await handler.Handle(
            new DeleteStockItemCommand(stockId, hogarId, usuarioId, ConsumoMotivos.Vencido),
            CancellationToken.None);

        Assert.True(result);
        var consumo = Assert.Single(consumos.Registros);
        Assert.Equal(ConsumoMotivos.Vencido, consumo.Motivo);
    }

    [Fact]
    public async Task Delete_WhenItemWithDescartadoMotivo_RegistersDescartadoConsumo()
    {
        var hogarId = Guid.NewGuid();
        var usuarioId = Guid.NewGuid();
        var stockId = Guid.NewGuid();
        var productoId = Guid.NewGuid();
        var repo = new FakeAlacenaRepository
        {
            DeleteResult = true,
            Items =
            [
                new StockItemResult(stockId, productoId, "Yogur", null, null, null, "Heladera", 1m, "unidad", null, false, 0, 1),
            ]
        };
        var consumos = new FakeConsumoRepository();
        var handler = new DeleteStockItemHandler(repo, consumos);

        var result = await handler.Handle(
            new DeleteStockItemCommand(stockId, hogarId, usuarioId, ConsumoMotivos.Descartado),
            CancellationToken.None);

        Assert.True(result);
        var consumo = Assert.Single(consumos.Registros);
        Assert.Equal(ConsumoMotivos.Descartado, consumo.Motivo);
    }

    private sealed class FakeAlacenaRepository : IAlacenaRepository
    {
        public IReadOnlyList<StockItemResult> Items { get; set; } = Array.Empty<StockItemResult>();
        public StockItemResult? CreatedResult { get; set; }
        public StockItemResult? UpdatedResult { get; set; }
        public bool DeleteResult { get; set; }
        public Guid LastGetByIdId { get; private set; }
        public Guid LastGetByIdHogarId { get; private set; }
        public Guid LastDeleteId { get; private set; }
        public Guid LastDeleteHogarId { get; private set; }

        public Task<IReadOnlyList<StockItemResult>> GetByHogarAsync(Guid hogarId, CancellationToken ct)
            => Task.FromResult(Items);

        public Task<StockItemResult?> GetByIdAsync(Guid id, Guid hogarId, CancellationToken ct)
        {
            LastGetByIdId = id;
            LastGetByIdHogarId = hogarId;
            return Task.FromResult(Items.FirstOrDefault(item => item.Id == id));
        }

        public Task<StockItemResult> CreateAsync(CreateStockItemRequestModel request, CancellationToken ct)
            => Task.FromResult(CreatedResult ??
                new StockItemResult(Guid.NewGuid(), Guid.NewGuid(), request.Nombre, request.Imagen, request.CodigoBarras, null, request.Ubicacion, request.Cantidad, request.UnidadMedida ?? "unidad", request.FechaVencimiento, request.EstaAbierto, request.PorcentajeConsumido, request.CantidadEnvases));

        public Task<StockItemResult?> UpdateAsync(UpdateStockItemRequestModel request, CancellationToken ct)
            => Task.FromResult(UpdatedResult);

        public Task<bool> DeleteAsync(Guid id, Guid hogarId, CancellationToken ct)
        {
            LastDeleteId = id;
            LastDeleteHogarId = hogarId;
            return Task.FromResult(DeleteResult);
        }
    }

    private sealed class FakeConsumoRepository : IConsumoProductoRepository
    {
        public List<RegistrarConsumoInput> Registros { get; } = [];

        public Task RegistrarAsync(RegistrarConsumoInput input, CancellationToken ct)
        {
            Registros.Add(input);
            return Task.CompletedTask;
        }

        public Task<IReadOnlyList<ConsumoPorProducto>> GetConsumosPorProductoAsync(
            Guid hogarId, int diasAtras, CancellationToken ct)
            => Task.FromResult<IReadOnlyList<ConsumoPorProducto>>(Array.Empty<ConsumoPorProducto>());

        public Task<IReadOnlyList<ConsumoMovimiento>> GetMovimientosAsync(
            Guid hogarId, ConsumoMovimientosFiltro filtro, CancellationToken ct)
            => Task.FromResult<IReadOnlyList<ConsumoMovimiento>>(Array.Empty<ConsumoMovimiento>());

        public Task<IReadOnlyList<ComprasPorProducto>> GetComprasPorProductoAsync(
            Guid hogarId, int diasAtras, CancellationToken ct)
            => Task.FromResult<IReadOnlyList<ComprasPorProducto>>(Array.Empty<ComprasPorProducto>());

        public Task<IReadOnlyDictionary<DayOfWeek, int>> GetCocinadasPorDiaSemanaAsync(
            Guid hogarId, int diasAtras, CancellationToken ct)
            => Task.FromResult<IReadOnlyDictionary<DayOfWeek, int>>(new Dictionary<DayOfWeek, int>());

        public Task<IReadOnlyList<RecetaTopItem>> GetRecetasTopAsync(
            Guid hogarId, int diasAtras, int topN, CancellationToken ct)
            => Task.FromResult<IReadOnlyList<RecetaTopItem>>(Array.Empty<RecetaTopItem>());

        public Task<IReadOnlyList<EnvaseZombieRaw>> GetEnvasesAbiertosLargoAsync(
            Guid hogarId, int diasMinAbierto, CancellationToken ct)
            => Task.FromResult<IReadOnlyList<EnvaseZombieRaw>>(Array.Empty<EnvaseZombieRaw>());

        public Task<IReadOnlyList<DateTime>> GetFechasComprasHogarAsync(
            Guid hogarId, int diasAtras, CancellationToken ct)
            => Task.FromResult<IReadOnlyList<DateTime>>(Array.Empty<DateTime>());
    }
}
