namespace Nido.Api.Contracts.ListaCompras;

public sealed record ListaCompraGrupoResponse(
    string GrupoNombre,
    IReadOnlyList<ListaCompraItemResponse> Items);

public sealed record ListaCompraItemResponse(
    Guid Id,
    Guid ProductoId,
    string Nombre,
    decimal? Cantidad,
    string? Unidad,
    bool Comprado,
    DateTime? CompradoEn,
    int Orden);

public sealed record ListaCompraHistorialItemResponse(
    Guid Id,
    Guid ProductoId,
    string Nombre,
    decimal? Cantidad,
    string? Unidad,
    string GrupoNombre,
    DateTime CompradoEn,
    Guid? CompradoPor);

