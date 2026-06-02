using Nido.Application.Alacena;
using Nido.Application.Alacena.Exceptions;

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
                new StockItemResult(Guid.NewGuid(), Guid.NewGuid(), "Arroz", null, "779", null, "Alacena", 1, "unidad", null, false, 0),
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
                new CreateStockItemCommand(Guid.NewGuid(), Guid.NewGuid(), "Arroz", null, null, "Alacena", 1, "no-fecha", false, 0),
                CancellationToken.None));
    }

    [Fact]
    public async Task Update_WhenNotExists_ReturnsNull()
    {
        var repo = new FakeAlacenaRepository { UpdatedResult = null };
        var handler = new UpdateStockItemHandler(repo);

        var result = await handler.Handle(
            new UpdateStockItemCommand(Guid.NewGuid(), Guid.NewGuid(), null, null, null, null, null),
            CancellationToken.None);

        Assert.Null(result);
    }

    [Fact]
    public async Task Delete_WhenExists_ReturnsTrue()
    {
        var repo = new FakeAlacenaRepository { DeleteResult = true };
        var handler = new DeleteStockItemHandler(repo);

        var result = await handler.Handle(new DeleteStockItemCommand(Guid.NewGuid()), CancellationToken.None);

        Assert.True(result);
    }

    private sealed class FakeAlacenaRepository : IAlacenaRepository
    {
        public IReadOnlyList<StockItemResult> Items { get; set; } = Array.Empty<StockItemResult>();
        public StockItemResult? CreatedResult { get; set; }
        public StockItemResult? UpdatedResult { get; set; }
        public bool DeleteResult { get; set; }

        public Task<IReadOnlyList<StockItemResult>> GetByHogarAsync(Guid hogarId, CancellationToken ct)
            => Task.FromResult(Items);

        public Task<StockItemResult?> GetByIdAsync(Guid id, Guid hogarId, CancellationToken ct)
            => Task.FromResult(Items.FirstOrDefault(item => item.Id == id));

        public Task<StockItemResult> CreateAsync(CreateStockItemRequestModel request, CancellationToken ct)
            => Task.FromResult(CreatedResult ??
                new StockItemResult(Guid.NewGuid(), Guid.NewGuid(), request.Nombre, request.Imagen, request.CodigoBarras, null, request.Ubicacion, request.Cantidad, "unidad", request.FechaVencimiento, request.EstaAbierto, request.PorcentajeConsumido));

        public Task<StockItemResult?> UpdateAsync(UpdateStockItemRequestModel request, CancellationToken ct)
            => Task.FromResult(UpdatedResult);

        public Task<bool> DeleteAsync(Guid id, CancellationToken ct)
            => Task.FromResult(DeleteResult);
    }
}
