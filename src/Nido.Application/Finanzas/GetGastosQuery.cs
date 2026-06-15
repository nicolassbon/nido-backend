namespace Nido.Application.Finanzas;

public sealed record GetGastosQuery(
    Guid HogarId,
    string? Desde,
    string? Hasta,
    string? Categoria
);
