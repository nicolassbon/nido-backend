namespace Nido.Application.Gamificacion;

public interface IGamificationUnlockMaterializer
{
    /// <summary>
    /// Counts currently completed tasks, computes current XP, determines all eligible configured
    /// levels, inserts any missing unlock rows, and returns only the levels newly inserted
    /// by this call (evolution signals).
    /// </summary>
    Task<IReadOnlyList<int>> MaterializeEligibleUnlocksAsync(Guid usuarioId, CancellationToken ct);
}
