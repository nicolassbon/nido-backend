namespace Nido.Domain.Productos;

public sealed class Producto
{
    private Producto(){
    }

    public Producto(string nombre, Guid categoriaId, string? codigoBarras, string? imagenUrl)
    {
        Id = Guid.NewGuid();
        Nombre = nombre;
        CategoriaId = categoriaId;
        CodigoBarras = codigoBarras;
        ImagenUrl = imagenUrl;
    }

    public Guid Id { get; private set; }

    public string Nombre { get; private set; } = string.Empty;

    public Guid CategoriaId { get; private set; }

    public string? CodigoBarras { get; private set; }

    public string? ImagenUrl { get; private set; }
}