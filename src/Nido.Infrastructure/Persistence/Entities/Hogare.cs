using System;
using System.Collections.Generic;

namespace Nido.Infrastructure.Persistence.Entities;

public partial class Hogare
{
    public Guid Id { get; set; }

    public string Nombre { get; set; } = null!;

    public DateTime CreatedAt { get; set; }

    public virtual ICollection<Electrodomestico> Electrodomesticos { get; set; } = new List<Electrodomestico>();

    public virtual ICollection<Gasto> Gastos { get; set; } = new List<Gasto>();

    public virtual ICollection<InvitacionesHogar> InvitacionesHogars { get; set; } = new List<InvitacionesHogar>();

    public virtual ICollection<ListaCompra> ListaCompras { get; set; } = new List<ListaCompra>();

    public virtual ICollection<LogrosHogar> LogrosHogars { get; set; } = new List<LogrosHogar>();

    public virtual ICollection<MiembrosHogar> MiembrosHogars { get; set; } = new List<MiembrosHogar>();

    public virtual ICollection<RecetasCocinada> RecetasCocinada { get; set; } = new List<RecetasCocinada>();

    public virtual ICollection<StockHogar> StockHogars { get; set; } = new List<StockHogar>();

    public virtual ICollection<Tarea> Tareas { get; set; } = new List<Tarea>();
}
