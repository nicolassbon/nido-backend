namespace Nido.Api.Contracts.Electrodomesticos;

public sealed record ElectrodomesticoResponse(
    Guid Id,
    Guid HogarId,
    string Nombre,
    string? Tipo,
    string? Estado,
    string? Marca,
    string? ImagenUrl,
    Guid? CatalogoId
);
