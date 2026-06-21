namespace Nido.Infrastructure.Persistence.Entities;

public partial class UbicacionCatalogo
{
    public Guid    Id     { get; set; }
    public string  Nombre { get; set; } = null!;
    public string? Icono  { get; set; }
    public string? Color  { get; set; }
}
