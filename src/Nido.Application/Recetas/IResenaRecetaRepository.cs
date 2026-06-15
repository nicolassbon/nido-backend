namespace Nido.Application.Recetas;

public sealed record ResenaItem(
    Guid     Id,
    Guid     RecetaId,
    Guid     HogarId,
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
    /// <summary>Crea o actualiza la reseña/nota del miembro para una receta dentro de su hogar.</summary>
    Task<ResenaItem> UpsertAsync(Guid recetaId, Guid hogarId, Guid usuarioId, int puntuacion, string? comentario, CancellationToken ct);

    /// <summary>Notas y calificaciones VISIBLES para el hogar (estilo "diario del hogar").</summary>
    Task<IReadOnlyList<ResenaItem>> GetByRecetaAndHogarAsync(Guid recetaId, Guid hogarId, CancellationToken ct);

    Task<ResenaItem?> GetByRecetaHogarUsuarioAsync(Guid recetaId, Guid hogarId, Guid usuarioId, CancellationToken ct);

    /// <summary>Borra la nota del miembro. Idempotente.</summary>
    Task DeleteAsync(Guid recetaId, Guid hogarId, Guid usuarioId, CancellationToken ct);

    /// <summary>Promedio de estrellas dentro del hogar.</summary>
    Task<ResenaResumen> GetResumenAsync(Guid recetaId, Guid hogarId, CancellationToken ct);

    /// <summary>Resumen por receta filtrado al hogar. Útil para tarjetas en listados.</summary>
    Task<IReadOnlyDictionary<Guid, ResenaResumen>> GetResumenesAsync(IEnumerable<Guid> recetaIds, Guid hogarId, CancellationToken ct);
}
