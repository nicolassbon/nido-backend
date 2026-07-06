namespace Nido.Infrastructure.Persistence.Entities;

public partial class GamificacionNivelDesbloqueado
{
    public Guid Id { get; set; }
    public Guid UsuarioId { get; set; }
    public int Nivel { get; set; }
    public DateTime DesbloqueadoEn { get; set; }

    public virtual Usuario Usuario { get; set; } = null!;
}
