namespace Nido.Infrastructure.Persistence.Entities;

public class NotaReceta
{
    public Guid Id { get; set; }
    public Guid RecetaId { get; set; }
    public Guid HogarId { get; set; }
    public Guid UsuarioId { get; set; }
    public string Texto { get; set; } = null!;
    public DateTime CreatedAt { get; set; }

    public virtual Receta Receta { get; set; } = null!;
    public virtual Usuario Usuario { get; set; } = null!;
    public virtual Hogare Hogar { get; set; } = null!;
}
