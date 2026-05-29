namespace Nido.Infrastructure.Persistence.Entities;

public partial class HogarMeta
{
    public Guid HogarId { get; set; }
    public Guid MetaId { get; set; }

    public virtual Hogare Hogar { get; set; } = null!;
    public virtual MetasCatalogo Meta { get; set; } = null!;
}
