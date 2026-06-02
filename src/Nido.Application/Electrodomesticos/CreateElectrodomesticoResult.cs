namespace Nido.Application.Electrodomesticos;

public sealed record CreateElectrodomesticoResult(
    Guid Id,
    Guid HogarId,
    string Nombre,
    string? Tipo,
    string? Estado,
    string? Marca,
    string? ImagenUrl
);