namespace Nido.Domain.Electrodomesticos;

public sealed class Electrodomestico
{
    private Electrodomestico()
    {
    }

    public Electrodomestico(Guid hogarId, string nombre, string? tipo, string? estado, string? marca, string? imagenUrl)
    {
        Id = Guid.NewGuid();
        HogarId = hogarId;
        Nombre = nombre;
        Tipo = tipo;
        Estado = estado;
        Marca = marca;
        ImagenUrl = imagenUrl;
    }

    public Electrodomestico(Guid id, Guid hogarId, string nombre, string? tipo, string? estado, string? marca, string? imagenUrl)
    {
        Id = id;
        HogarId = hogarId;
        Nombre = nombre;
        Tipo = tipo;
        Estado = estado;
        Marca = marca;
        ImagenUrl = imagenUrl;
    }

    public Guid Id { get; private set; }

    public Guid HogarId { get; private set; }

    public string Nombre { get; private set; } = string.Empty;

    public string? Tipo { get; private set; }

    public string? Estado { get; private set; }

    public string? Marca { get; set; }

public string? ImagenUrl { get; set; }
}