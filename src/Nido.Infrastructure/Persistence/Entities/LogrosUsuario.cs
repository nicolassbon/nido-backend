using System;
using System.Collections.Generic;

namespace Nido.Infrastructure.Persistence.Entities;

public partial class LogrosUsuario
{
    public Guid Id { get; set; }

    public Guid UsuarioId { get; set; }

    public Guid LogroId { get; set; }

    public DateTime FechaObtenido { get; set; }

    public virtual Logro Logro { get; set; } = null!;

    public virtual Usuario Usuario { get; set; } = null!;
}
