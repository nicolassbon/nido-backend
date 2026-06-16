namespace Nido.Application.Finanzas;

public sealed record GetFacturasQuery(
    Guid HogarId,
    string? Tipo,
    bool? Pagada,
    int? ProximaDias
);
