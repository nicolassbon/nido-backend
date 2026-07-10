namespace Nido.Infrastructure.Persistence.Entities;

public partial class TutorialUsuario
{
    public Guid Id { get; set; }
    public Guid UsuarioId { get; set; }
    public bool HomeCompletado { get; set; }
    public bool AlacenaCompletado { get; set; }
    public bool RecetasCompletado { get; set; }
    public bool ListaComprasCompletado { get; set; }
    public bool ElectrodomesticosCompletado { get; set; }
    public bool FinanzasCompletado { get; set; }
    public bool PlanificadorCompletado { get; set; }
    public bool TareasCompletado { get; set; }
    public bool NotificacionesCompletado { get; set; }
    public bool PerfilCompletado { get; set; }
    public bool ConfiguracionCompletado { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public virtual Usuario Usuario { get; set; } = null!;
}
