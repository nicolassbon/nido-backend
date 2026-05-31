namespace Nido.Domain.Electrodomesticos;

public sealed class ElectrodomesticoCatalogo
{
    public ElectrodomesticoCatalogo(
        Guid id,
        string nombre,
        string tipo,
        string? icono,
        string? imagenUrl,
        int orden,
        bool activo)
    {
        Id = id;
        Nombre = nombre;
        Tipo = tipo;
        Icono = icono;
        ImagenUrl = imagenUrl;
        Orden = orden;
        Activo = activo;
    }

    public Guid Id { get; }
    public string Nombre { get; }
    public string Tipo { get; }
    public string? Icono { get; }
    public string? ImagenUrl { get; }
    public int Orden { get; }
    public bool Activo { get; }
}