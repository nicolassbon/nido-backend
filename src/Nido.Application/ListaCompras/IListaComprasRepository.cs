namespace Nido.Application.ListaCompras;

public interface IListaComprasRepository
{
    Task<IReadOnlyList<ListaCompraListResult>> GetListsAsync(Guid hogarId, CancellationToken ct);

    Task<ListaCompraListResult> CreateListAsync(Guid hogarId, Guid usuarioId, string nombre, CancellationToken ct);

    Task<ListaCompraListResult?> UpdateListAsync(Guid hogarId, Guid listaId, string nombre, CancellationToken ct);

    Task<bool> DeleteListAsync(Guid hogarId, Guid listaId, CancellationToken ct);

    Task<ListaCompraItemResult?> AddItemToListAsync(
        Guid hogarId,
        Guid listaId,
        Guid usuarioId,
        string nombre,
        decimal? cantidad,
        string? unidad,
        CancellationToken ct);

    Task<ListaCompraItemResult?> UpdateItemAsync(
        Guid hogarId,
        Guid listaId,
        Guid itemId,
        string? nombre,
        decimal? cantidad,
        string? unidad,
        bool? comprado,
        Guid usuarioId,
        CancellationToken ct);

    Task<bool> RemoveItemAsync(Guid id, Guid hogarId, Guid listaId, CancellationToken ct);

    Task<IReadOnlyList<ListaCompraGrupoResult>> GetActiveAsync(Guid hogarId, CancellationToken ct);

    Task<IReadOnlyList<ListaCompraGrupoResult>> GetActiveByListAsync(Guid hogarId, Guid listaId, CancellationToken ct);

    Task<IReadOnlyList<ListaCompraHistorialItemResult>> GetHistorialAsync(Guid hogarId, CancellationToken ct);

    Task<IReadOnlyList<ListaCompraGrupoResult>> ReplaceGroupAsync(
        Guid hogarId,
        Guid usuarioId,
        string grupoNombre,
        IReadOnlyList<ListaCompraItemInput> items,
        CancellationToken ct);

    Task<ListaCompraItemResult> AddItemAsync(
        Guid hogarId,
        Guid usuarioId,
        string nombre,
        decimal? cantidad,
        string? unidad,
        string grupoNombre,
        CancellationToken ct);

    Task<ListaCompraItemResult?> MarkPurchasedAsync(Guid id, Guid hogarId, Guid usuarioId, CancellationToken ct);

    Task<IReadOnlyList<ListaCompraItemResult>> MarkPurchasedByNameAsync(
        Guid hogarId,
        Guid usuarioId,
        string nombre,
        CancellationToken ct);

    Task<bool> MarkAddedToInventoryAsync(Guid id, Guid hogarId, CancellationToken ct);

    Task<bool> RemoveItemAsync(Guid id, Guid hogarId, CancellationToken ct);

    Task ClearActiveAsync(Guid hogarId, CancellationToken ct);
}
