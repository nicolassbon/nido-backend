using System;
using System.Collections.Generic;

namespace Nido.Api.Contracts.UsuariosPerfil;

public sealed record ActualizarRestriccionesRequest(
    string Tipo,
    List<Guid> RestriccionIds
);
