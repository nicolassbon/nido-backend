namespace Nido.Application.ListaCompras;

public sealed record ListaCompraItemInput(
    string Nombre,
    decimal? Cantidad,
    string? Unidad);

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

