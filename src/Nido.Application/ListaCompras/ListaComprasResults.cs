namespace Nido.Application.ListaCompras;

public sealed record ListaCompraGrupoResult(
    string GrupoNombre,
    IReadOnlyList<ListaCompraItemResult> Items);

public sealed record ListaCompraListResult(
    Guid Id,
    string Nombre,
    DateTime CreatedAt,
    DateTime? UpdatedAt,
    IReadOnlyList<ListaCompraItemResult> Items);

public sealed record ListaCompraItemResult(
    Guid Id,
    Guid? ProductoId,
    string Nombre,
    decimal? Cantidad,
    string? Unidad,
    bool Comprado,
    DateTime? CompradoEn,
    int Orden,
    string? CategoriaNombre = null,
    string? IconoSvg = null,
    string? Icono = null);

public sealed record ListaCompraHistorialItemResult(
    Guid Id,
    Guid? ProductoId,
    string Nombre,
    decimal? Cantidad,
    string? Unidad,
    string GrupoNombre,
    DateTime CompradoEn,
    Guid? CompradoPor,
    bool AgregadoAlInventario,
    string? CategoriaNombre = null,
    string? IconoSvg = null,
    string? Icono = null);

public enum SendListaCompraToTelegramStatus
{
    Enqueued,
    Empty,
    NoTelegramLink
}

public sealed record SendListaCompraToTelegramResult(
    SendListaCompraToTelegramStatus Status,
    int ItemCount,
    long? ChatId,
    Guid? ListaId);
