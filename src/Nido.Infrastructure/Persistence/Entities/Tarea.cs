using System;
using System.Collections.Generic;

namespace Nido.Infrastructure.Persistence.Entities;

public partial class Tarea
{
    public Guid Id { get; set; }

    public Guid HogarId { get; set; }

    public Guid CreadoPor { get; set; }

    public string Titulo { get; set; } = null!;

    public string? Descripcion { get; set; }

    public string? Estado { get; set; }

    public DateTime? FechaLimite { get; set; }

    public Guid? CompletadoPor { get; set; }

    public DateTime? FechaCompletado { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual ICollection<AsignacionesTarea> AsignacionesTareas { get; set; } = new List<AsignacionesTarea>();
    public virtual ICollection<PlanificadorItem> PlanificadorItems { get; set; } = new List<PlanificadorItem>();

    public virtual Usuario? CompletadoPorNavigation { get; set; }

    public virtual Usuario CreadoPorNavigation { get; set; } = null!;

    public virtual Hogare Hogar { get; set; } = null!;
}
