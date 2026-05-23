namespace Nido.Application.Electrodomesticos;

public sealed record CreateElectrodomesticoCommand(
    Guid HogarId,
    string Nombre,
    string? Tipo,
    string? Estado
);