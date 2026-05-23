using System;
using System.Collections.Generic;

namespace Nido.Infrastructure.Persistence.Entities;

public partial class LogrosHogar
{
    public Guid Id { get; set; }

    public Guid HogarId { get; set; }

    public Guid LogroId { get; set; }

    public virtual Hogare Hogar { get; set; } = null!;

    public virtual Logro Logro { get; set; } = null!;
}
