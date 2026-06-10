namespace Nido.Application.Recetas;

public sealed record ResenaItem(
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

public sealed record ResenaResumen(decimal Promedio, int Total);

public interface IResenaRecetaRepository
{
    /// <summary>Crea o actualiza la reseña del usuario para una receta (upsert).</summary>
    Task<ResenaItem> UpsertAsync(Guid recetaId, Guid usuarioId, int puntuacion, string? comentario, CancellationToken ct);

    Task<IReadOnlyList<ResenaItem>> GetByRecetaAsync(Guid recetaId, CancellationToken ct);

    Task<ResenaItem?> GetByRecetaAndUsuarioAsync(Guid recetaId, Guid usuarioId, CancellationToken ct);

    /// <summary>Borra la reseña del usuario para una receta. Idempotente.</summary>
    Task DeleteAsync(Guid recetaId, Guid usuarioId, CancellationToken ct);

    Task<ResenaResumen> GetResumenAsync(Guid recetaId, CancellationToken ct);

    /// <summary>Devuelve el resumen (promedio + total) por receta para una lista. Util para mostrar en tarjetas.</summary>
    Task<IReadOnlyDictionary<Guid, ResenaResumen>> GetResumenesAsync(IEnumerable<Guid> recetaIds, CancellationToken ct);
}
