using System;
using System.Collections.Generic;

namespace Nido.Infrastructure.Persistence.Entities;

public partial class Logro
{
    public Guid Id { get; set; }

    public string Nombre { get; set; } = null!;

    public string? Descripcion { get; set; }

    public string? IconoUrl { get; set; }

    public virtual ICollection<LogrosHogar> LogrosHogars { get; set; } = new List<LogrosHogar>();

    public virtual ICollection<LogrosUsuario> LogrosUsuarios { get; set; } = new List<LogrosUsuario>();
}
