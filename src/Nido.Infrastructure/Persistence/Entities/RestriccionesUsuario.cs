namespace Nido.Infrastructure.Persistence.Entities;

public partial class RestriccionesUsuario
{
    public Guid UsuarioId { get; set; }
    public Guid RestriccionId { get; set; }

    public virtual Usuario Usuario { get; set; } = null!;
    public virtual RestriccionesCatalogo Restriccion { get; set; } = null!;
}
