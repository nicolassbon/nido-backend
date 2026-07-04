namespace Nido.Application.Electrodomesticos;

public sealed record UpdateElectrodomesticoCommand(
    Guid Id,
    Guid HogarId,
    string? Tipo,
    string? Estado
);
