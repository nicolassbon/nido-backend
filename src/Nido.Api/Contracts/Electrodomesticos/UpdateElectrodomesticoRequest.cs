namespace Nido.Api.Contracts.Electrodomesticos;

public sealed record UpdateElectrodomesticoRequest(
    string? Tipo,
    string? Estado
);
