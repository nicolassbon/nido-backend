using System;
using System.Collections.Generic;

namespace Nido.Infrastructure.Persistence.Entities;

public partial class Notificacione
{
    public Guid Id { get; set; }

    public Guid UsuarioId { get; set; }

    public string? Tipo { get; set; }

    public string? Mensaje { get; set; }

    public bool? Leida { get; set; }

    public Guid? ReferenciaId { get; set; }

    public string? ReferenciaTipo { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual Usuario Usuario { get; set; } = null!;
}
