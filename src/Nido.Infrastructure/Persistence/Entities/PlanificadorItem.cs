namespace Nido.Infrastructure.Persistence.Entities;

public partial class PlanificadorItem
{
    public Guid     Id          { get; set; }
    public Guid     SemanaId    { get; set; }
    public DateOnly Fecha       { get; set; }
    public string   TipoComida  { get; set; } = null!; // desayuno | almuerzo | cena | tarea
    public Guid?    RecetaId    { get; set; }
    public string?  TituloLibre { get; set; }
    public string?  ImagenUrl   { get; set; }
    public string?  Hora        { get; set; }
    public int      Orden       { get; set; }
    public Guid     CreadoPor   { get; set; }
    public DateTime CreatedAt   { get; set; }

    public virtual PlanificadorSemana Semana { get; set; } = null!;
    public virtual Receta?            Receta { get; set; }
    public virtual Usuario            CreadoPorNavigation { get; set; } = null!;
}
