using System;
using System.Collections.Generic;

namespace Nido.Infrastructure.Persistence.Entities;

public partial class Electrodomestico
{
    public Guid Id { get; set; }

    public Guid HogarId { get; set; }

    public string Nombre { get; set; } = null!;

    public string? Tipo { get; set; }

    public string? Estado { get; set; }

    public virtual Hogare Hogar { get; set; } = null!;
}
