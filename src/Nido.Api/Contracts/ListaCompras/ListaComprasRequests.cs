namespace Nido.Api.Contracts.ListaCompras;

public sealed record AddListaCompraGroupRequest(
    string GrupoNombre,
    IReadOnlyList<AddListaCompraItemRequest> Items);

public sealed record AddListaCompraItemRequest(
    string Nombre,
    decimal? Cantidad,
    string? Unidad,
    string? GrupoNombre);

public sealed record MarkListaCompraItemByNameRequest(string Nombre);

