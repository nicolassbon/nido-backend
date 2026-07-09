namespace Nido.Infrastructure.Persistence.Entities;

public class GastoParticipante
{
    public Guid GastoId { get; set; }
    public Guid UsuarioId { get; set; }

    public virtual Gasto Gasto { get; set; } = null!;
    public virtual Usuario Usuario { get; set; } = null!;
}
