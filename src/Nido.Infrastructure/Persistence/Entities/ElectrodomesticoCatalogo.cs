using System;
using System.Collections.Generic;

namespace Nido.Infrastructure.Persistence.Entities;

public partial class ElectrodomesticoCatalogo
{
    public Guid Id { get; set; }
    public string Nombre { get; set; } = null!;
    public string Tipo { get; set; } = null!;
    public string? Icono { get; set; }
    public string? ImagenUrl { get; set; }
    public int Orden { get; set; }
    public bool Activo { get; set; }

    public virtual ICollection<Electrodomestico> Electrodomesticos { get; set; } = [];
}
