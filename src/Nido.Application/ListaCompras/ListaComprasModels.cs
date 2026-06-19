namespace Nido.Application.ListaCompras;

public sealed record ListaCompraItemInput(
    string Nombre,
    decimal? Cantidad,
    string? Unidad);

public sealed record CreateListaCompraCommand(Guid HogarId, Guid UsuarioId, string Nombre);

public sealed record UpdateListaCompraCommand(Guid HogarId, Guid ListaId, string Nombre);

public sealed record DeleteListaCompraCommand(Guid HogarId, Guid ListaId);

public sealed record AddListaCompraNamedItemCommand(
    Guid HogarId,
    Guid ListaId,
    Guid UsuarioId,
    string Nombre,
    decimal? Cantidad,
    string? Unidad);

public sealed record UpdateListaCompraItemCommand(
    Guid HogarId,
    Guid ListaId,
    Guid ItemId,
    Guid UsuarioId,
    string? Nombre,
    decimal? Cantidad,
    string? Unidad,
    bool? Comprado);

public sealed record RemoveListaCompraNamedItemCommand(Guid Id, Guid HogarId, Guid ListaId);

public sealed record AddListaCompraGroupCommand(
    Guid HogarId,
    Guid UsuarioId,
    string GrupoNombre,
    IReadOnlyList<ListaCompraItemInput> Items);

public sealed record AddListaCompraItemCommand(
    Guid HogarId,
    Guid UsuarioId,
    string Nombre,
    decimal? Cantidad,
    string? Unidad,
    string? GrupoNombre);

public sealed record MarkListaCompraItemCompradoCommand(
    Guid Id,
    Guid HogarId,
    Guid UsuarioId);

public sealed record MarkListaCompraItemCompradoByNameCommand(
    Guid HogarId,
    Guid UsuarioId,
    string Nombre);

public sealed record RemoveListaCompraItemCommand(Guid Id, Guid HogarId);

public sealed record ClearListaComprasCommand(Guid HogarId);

