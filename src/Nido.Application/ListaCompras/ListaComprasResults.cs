namespace Nido.Application.ListaCompras;

public sealed record ListaCompraGrupoResult(
    string GrupoNombre,
    IReadOnlyList<ListaCompraItemResult> Items);

public sealed record ListaCompraItemResult(
    Guid Id,
    Guid ProductoId,
    string Nombre,
    decimal? Cantidad,
    string? Unidad,
    bool Comprado,
    DateTime? CompradoEn,
    int Orden);

public sealed record ListaCompraHistorialItemResult(
    Guid Id,
    Guid ProductoId,
    string Nombre,
    decimal? Cantidad,
    string? Unidad,
    string GrupoNombre,
    DateTime CompradoEn,
    Guid? CompradoPor);

