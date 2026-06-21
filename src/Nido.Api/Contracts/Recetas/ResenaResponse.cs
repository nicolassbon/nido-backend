namespace Nido.Api.Contracts.Recetas;

public sealed record ResenaResponse(
    Guid     Id,
    Guid     RecetaId,
    Guid     UsuarioId,
    string   UsuarioNombre,
    string?  UsuarioFotoUrl,
    int      Puntuacion,
    string?  Comentario,
    DateTime CreatedAt,
    DateTime? UpdatedAt
);

public sealed record ResenasRecetaResponse(
    IReadOnlyList<ResenaResponse> Items,
    decimal Promedio,
    int     Total,
    ResenaResponse? MiResena
);
