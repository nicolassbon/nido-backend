using System;
using System.Collections.Generic;

namespace Nido.Infrastructure.Persistence.Entities;

public partial class ReseniasRecetum
{
    public Guid Id { get; set; }

    public Guid RecetaId { get; set; }

    public Guid UsuarioId { get; set; }

    public int? Puntuacion { get; set; }

    public string? Comentario { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public virtual Receta Receta { get; set; } = null!;

    public virtual Usuario Usuario { get; set; } = null!;
}
