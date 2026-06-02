namespace Nido.Application.Electrodomesticos;

public sealed record CreateElectrodomesticoCommand(
    Guid HogarId,
    Guid? CatalogoId,
    string? Nombre,
    string? Tipo,
    string? Estado,
    string? Marca,
    string? ImagenUrl
);
