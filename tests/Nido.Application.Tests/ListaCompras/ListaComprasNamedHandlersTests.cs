using Nido.Application.ListaCompras;

namespace Nido.Application.Tests.ListaCompras;

public sealed class ListaComprasNamedHandlersTests
{
    [Fact]
    public async Task CreateList_NormalizesNameAndCallsRepository()
    {
        var repo = new FakeListaComprasRepository();
        var handler = new CreateListaCompraHandler(repo);
        var hogarId = Guid.NewGuid();
        var usuarioId = Guid.NewGuid();

        var result = await handler.Handle(new CreateListaCompraCommand(hogarId, usuarioId, "  Feria  "), CancellationToken.None);

        Assert.Equal("Feria", repo.CreatedName);
        Assert.Equal(hogarId, repo.CreatedHogarId);
        Assert.Equal(usuarioId, repo.CreatedUsuarioId);
        Assert.Equal("Feria", result.Nombre);
    }

    [Fact]
    public async Task AddItemToList_TrimsNameAndUnit()
    {
        var repo = new FakeListaComprasRepository();
        var handler = new AddListaCompraNamedItemHandler(repo);
        var listaId = Guid.NewGuid();

        await handler.Handle(
            new AddListaCompraNamedItemCommand(Guid.NewGuid(), listaId, Guid.NewGuid(), "  Tomate  ", 2, " kg "),
            CancellationToken.None);

        Assert.Equal(listaId, repo.LastItemListId);
        Assert.Equal("Tomate", repo.LastItemName);
        Assert.Equal("kg", repo.LastItemUnit);
    }

    [Fact]
    public async Task RemoveNamedItem_SendsListScope()
    {
        var repo = new FakeListaComprasRepository();
        var handler = new RemoveListaCompraNamedItemHandler(repo);
        var itemId = Guid.NewGuid();
        var hogarId = Guid.NewGuid();
        var listaId = Guid.NewGuid();

        var removed = await handler.Handle(new RemoveListaCompraNamedItemCommand(itemId, hogarId, listaId), CancellationToken.None);

        Assert.True(removed);
        Assert.Equal(itemId, repo.RemovedItemId);
        Assert.Equal(hogarId, repo.RemovedHogarId);
        Assert.Equal(listaId, repo.RemovedListId);
    }

    private sealed class FakeListaComprasRepository : IListaComprasRepository
    {
        public Guid CreatedHogarId { get; private set; }
        public Guid CreatedUsuarioId { get; private set; }
        public string? CreatedName { get; private set; }
        public Guid LastItemListId { get; private set; }
        public string? LastItemName { get; private set; }
        public string? LastItemUnit { get; private set; }
        public Guid RemovedItemId { get; private set; }
        public Guid RemovedHogarId { get; private set; }
        public Guid RemovedListId { get; private set; }

        public Task<IReadOnlyList<ListaCompraListResult>> GetListsAsync(Guid hogarId, CancellationToken ct)
            => Task.FromResult<IReadOnlyList<ListaCompraListResult>>(Array.Empty<ListaCompraListResult>());

        public Task<ListaCompraListResult> CreateListAsync(Guid hogarId, Guid usuarioId, string nombre, CancellationToken ct)
        {
            CreatedHogarId = hogarId;
            CreatedUsuarioId = usuarioId;
            CreatedName = nombre;
            return Task.FromResult(new ListaCompraListResult(Guid.NewGuid(), nombre, DateTime.UtcNow, null, []));
        }

        public Task<ListaCompraListResult?> UpdateListAsync(Guid hogarId, Guid listaId, string nombre, CancellationToken ct)
            => Task.FromResult<ListaCompraListResult?>(new ListaCompraListResult(listaId, nombre, DateTime.UtcNow, DateTime.UtcNow, []));

        public Task<bool> DeleteListAsync(Guid hogarId, Guid listaId, CancellationToken ct)
            => Task.FromResult(true);

        public Task<ListaCompraItemResult?> AddItemToListAsync(Guid hogarId, Guid listaId, Guid usuarioId, string nombre, decimal? cantidad, string? unidad, CancellationToken ct)
        {
            LastItemListId = listaId;
            LastItemName = nombre;
            LastItemUnit = unidad;
            return Task.FromResult<ListaCompraItemResult?>(new ListaCompraItemResult(Guid.NewGuid(), null, nombre, cantidad, unidad, false, null, 0));
        }

        public Task<ListaCompraItemResult?> UpdateItemAsync(Guid hogarId, Guid listaId, Guid itemId, string? nombre, decimal? cantidad, string? unidad, bool? comprado, Guid usuarioId, CancellationToken ct)
            => Task.FromResult<ListaCompraItemResult?>(new ListaCompraItemResult(itemId, null, nombre ?? "Item", cantidad, unidad, comprado ?? false, null, 0));

        public Task<bool> RemoveItemAsync(Guid id, Guid hogarId, Guid listaId, CancellationToken ct)
        {
            RemovedItemId = id;
            RemovedHogarId = hogarId;
            RemovedListId = listaId;
            return Task.FromResult(true);
        }

        public Task<IReadOnlyList<ListaCompraGrupoResult>> GetActiveAsync(Guid hogarId, CancellationToken ct)
            => Task.FromResult<IReadOnlyList<ListaCompraGrupoResult>>(Array.Empty<ListaCompraGrupoResult>());
        public Task<IReadOnlyList<ListaCompraGrupoResult>> GetActiveByListAsync(Guid hogarId, Guid listaId, CancellationToken ct)
            => Task.FromResult<IReadOnlyList<ListaCompraGrupoResult>>(Array.Empty<ListaCompraGrupoResult>());
        public Task<IReadOnlyList<ListaCompraHistorialItemResult>> GetHistorialAsync(Guid hogarId, CancellationToken ct)
            => Task.FromResult<IReadOnlyList<ListaCompraHistorialItemResult>>(Array.Empty<ListaCompraHistorialItemResult>());
        public Task<IReadOnlyList<ListaCompraGrupoResult>> ReplaceGroupAsync(Guid hogarId, Guid usuarioId, string grupoNombre, IReadOnlyList<ListaCompraItemInput> items, CancellationToken ct)
            => Task.FromResult<IReadOnlyList<ListaCompraGrupoResult>>(Array.Empty<ListaCompraGrupoResult>());
        public Task<ListaCompraItemResult> AddItemAsync(Guid hogarId, Guid usuarioId, string nombre, decimal? cantidad, string? unidad, string grupoNombre, CancellationToken ct)
            => Task.FromResult(new ListaCompraItemResult(Guid.NewGuid(), null, nombre, cantidad, unidad, false, null, 0));
        public Task<ListaCompraItemResult?> MarkPurchasedAsync(Guid id, Guid hogarId, Guid usuarioId, CancellationToken ct)
            => Task.FromResult<ListaCompraItemResult?>(null);
        public Task<IReadOnlyList<ListaCompraItemResult>> MarkPurchasedByNameAsync(Guid hogarId, Guid usuarioId, string nombre, CancellationToken ct)
            => Task.FromResult<IReadOnlyList<ListaCompraItemResult>>(Array.Empty<ListaCompraItemResult>());
        public Task<bool> MarkAddedToInventoryAsync(Guid id, Guid hogarId, CancellationToken ct)
            => Task.FromResult(true);
        public Task<bool> RemoveItemAsync(Guid id, Guid hogarId, CancellationToken ct)
            => Task.FromResult(true);
        public Task ClearActiveAsync(Guid hogarId, CancellationToken ct)
            => Task.CompletedTask;
    }
}
