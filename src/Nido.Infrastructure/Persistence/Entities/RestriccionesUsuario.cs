using System;
using System.Collections.Generic;

namespace Nido.Infrastructure.Persistence.Entities;

public partial class RestriccionesUsuario
{
    public Guid Id { get; set; }

    public Guid UsuarioId { get; set; }

    public string? Tipo { get; set; }

    public string? Descripcion { get; set; }

    public virtual Usuario Usuario { get; set; } = null!;
}
