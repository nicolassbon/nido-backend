using System;
using System.Collections.Generic;

namespace Nido.Infrastructure.Persistence.Entities;

public partial class ListaCompraHogar
{
    public Guid Id { get; set; }

    public Guid HogarId { get; set; }

    public string Nombre { get; set; } = null!;

    public Guid CreadaPor { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public virtual Hogare Hogar { get; set; } = null!;

    public virtual Usuario CreadaPorNavigation { get; set; } = null!;

    public virtual ICollection<ListaCompra> Items { get; set; } = new List<ListaCompra>();
}
