namespace Nido.Infrastructure.Persistence.Entities;

public partial class PlanificadorSemana
{
    public Guid     Id          { get; set; }
    public Guid     HogarId     { get; set; }
    public DateOnly FechaInicio { get; set; }
    public DateTime CreatedAt   { get; set; }

    public virtual Hogare Hogar { get; set; } = null!;
    public virtual ICollection<PlanificadorItem> Items { get; set; } = new List<PlanificadorItem>();
}
