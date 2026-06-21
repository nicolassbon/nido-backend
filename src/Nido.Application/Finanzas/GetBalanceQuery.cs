namespace Nido.Application.Finanzas;

public sealed record GetBalanceQuery(
    Guid HogarId,
    string? Desde,
    string? Hasta
);
