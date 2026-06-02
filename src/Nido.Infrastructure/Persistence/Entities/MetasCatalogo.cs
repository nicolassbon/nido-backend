namespace Nido.Infrastructure.Persistence.Entities;

public partial class MetasCatalogo
{
    public Guid Id { get; set; }
    public string Nombre { get; set; } = null!;

    public virtual ICollection<HogarMeta> HogarMetas { get; set; } = [];
}
