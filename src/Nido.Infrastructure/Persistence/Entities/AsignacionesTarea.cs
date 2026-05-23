using System;
using System.Collections.Generic;

namespace Nido.Infrastructure.Persistence.Entities;

public partial class AsignacionesTarea
{
    public Guid Id { get; set; }

    public Guid TareaId { get; set; }

    public Guid UsuarioId { get; set; }

    public DateTime FechaAsignacion { get; set; }

    public virtual Tarea Tarea { get; set; } = null!;

    public virtual Usuario Usuario { get; set; } = null!;
}
