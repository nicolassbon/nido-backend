namespace Nido.Infrastructure.Persistence.Entities;

public partial class RestriccionesCatalogo
{
    public Guid Id { get; set; }
    public string Nombre { get; set; } = null!;
    public string Tipo { get; set; } = null!;

    public virtual ICollection<RestriccionesUsuario> RestriccionesUsuarios { get; set; } = [];
}
