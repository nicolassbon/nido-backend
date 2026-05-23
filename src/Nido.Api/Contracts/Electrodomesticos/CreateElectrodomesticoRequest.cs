namespace Nido.Api.Contracts.Electrodomesticos;

public sealed record CreateElectrodomesticoRequest(
    Guid HogarId,
    string? Nombre,
    string? Tipo,
    string? Estado
);