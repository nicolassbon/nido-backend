namespace Nido.Api.Contracts.Electrodomesticos;

public sealed record ElectrodomesticoCatalogoResponse(
    Guid Id,
    string Nombre,
    string Tipo,
    string? Icono,
    string? ImagenUrl,
    int Orden);