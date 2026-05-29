using System;
using System.Collections.Generic;

namespace Nido.Infrastructure.Persistence.Entities;

public partial class InvitacionesHogar
{
    public Guid Id { get; set; }

    public Guid HogarId { get; set; }

    public Guid InvitadoPor { get; set; }

    public string? Codigo { get; set; }

    public string? Token { get; set; }

    public string? EmailInvitado { get; set; }

    public string? Estado { get; set; }

    public DateTime? ExpiraEn { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual Hogare Hogar { get; set; } = null!;

    public virtual Usuario InvitadoPorNavigation { get; set; } = null!;
}
