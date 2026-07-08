namespace Nido.Application.Gamificacion;

public interface IGamificationRepository
{
    Task<int> CountCurrentlyCompletedTasksAsync(Guid usuarioId, CancellationToken ct);
    Task<IReadOnlyList<int>> GetUnlockedLevelsAsync(Guid usuarioId, CancellationToken ct);
    /// <summary>
    /// Atomically inserts missing unlock rows and returns only the levels newly inserted
    /// by this specific call. Must handle unique conflicts as success/no-op.
    /// </summary>
    Task<IReadOnlyList<int>> InsertMissingUnlocksAsync(Guid usuarioId, IEnumerable<int> levels, DateTime unlockedAt, CancellationToken ct);
}
