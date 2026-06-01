namespace Nido.Infrastructure.Persistence.Entities;

public partial class PasoReceta
{
    public Guid Id { get; set; }

    public Guid RecetaId { get; set; }

    public int Orden { get; set; }

    public string Descripcion { get; set; } = null!;

    public virtual Receta Receta { get; set; } = null!;
}