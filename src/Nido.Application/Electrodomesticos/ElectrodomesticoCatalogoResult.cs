namespace Nido.Application.Electrodomesticos;

public sealed record ElectrodomesticoCatalogoResult(
    Guid Id,
    string Nombre,
    string Tipo,
    string? Icono,
    string? ImagenUrl,
    int Orden);