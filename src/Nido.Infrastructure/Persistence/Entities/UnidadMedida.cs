namespace Nido.Infrastructure.Persistence.Entities;

public partial class UnidadMedida
{
    public Guid   Id     { get; set; }
    public string Codigo { get; set; } = null!;
    public string Nombre { get; set; } = null!;
}
