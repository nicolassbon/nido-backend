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

public sealed record CreateListaCompraRequest(string Nombre);

public sealed record UpdateListaCompraRequest(string Nombre);

public sealed record AddListaCompraNamedItemRequest(
    string Nombre,
    decimal? Cantidad,
    string? Unidad);

public sealed record UpdateListaCompraItemRequest(
    string? Nombre,
    decimal? Cantidad,
    string? Unidad,
    bool? Comprado);

