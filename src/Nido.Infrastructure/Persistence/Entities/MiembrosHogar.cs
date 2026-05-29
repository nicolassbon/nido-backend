using System;
using System.Collections.Generic;

namespace Nido.Infrastructure.Persistence.Entities;

public partial class MiembrosHogar
{
    public Guid Id { get; set; }

    public Guid UsuarioId { get; set; }

    public Guid HogarId { get; set; }

    public string? Rol { get; set; }

    public int? Puntos { get; set; }

    public string? NombreRepresentado { get; set; }

    public virtual Hogare Hogar { get; set; } = null!;

    public virtual Usuario Usuario { get; set; } = null!;
}
