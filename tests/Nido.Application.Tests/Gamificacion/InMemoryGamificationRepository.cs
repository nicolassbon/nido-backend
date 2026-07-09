using Nido.Application.Gamificacion;

namespace Nido.Application.Tests.Gamificacion;

/// <summary>
/// In-memory fake for IGamificationRepository used in materializer tests.
/// Tracks unlocked levels per user and simulates insert behavior including
/// unique conflict handling.
/// </summary>
public sealed class InMemoryGamificationRepository : IGamificationRepository
{
    private readonly Dictionary<Guid, HashSet<int>> _unlocks = new();
    private int _alreadyCompletedCount;

    public InMemoryGamificationRepository WithCompletedCount(int count)
    {
        _alreadyCompletedCount = count;
        return this;
    }

    public InMemoryGamificationRepository WithUnlockedLevels(Guid usuarioId, params int[] levels)
    {
        if (!_unlocks.ContainsKey(usuarioId))
            _unlocks[usuarioId] = new HashSet<int>();
        foreach (var l in levels)
            _unlocks[usuarioId].Add(l);
        return this;
    }

    private bool _throwUnexpectedInsertFailure;

    public InMemoryGamificationRepository WithUnexpectedInsertFailure()
    {
        _throwUnexpectedInsertFailure = true;
        return this;
    }

    public Task<int> CountCurrentlyCompletedTasksAsync(Guid usuarioId, CancellationToken ct)
        => Task.FromResult(_alreadyCompletedCount);

    public Task<IReadOnlyList<int>> GetUnlockedLevelsAsync(Guid usuarioId, CancellationToken ct)
    {
        if (_unlocks.TryGetValue(usuarioId, out var levels))
            return Task.FromResult<IReadOnlyList<int>>(levels.ToList());
        return Task.FromResult<IReadOnlyList<int>>(Array.Empty<int>());
    }

    public Task<IReadOnlyList<int>> InsertMissingUnlocksAsync(
        Guid usuarioId, IEnumerable<int> levels, DateTime unlockedAt, CancellationToken ct)
    {
        if (_throwUnexpectedInsertFailure)
            throw new InvalidOperationException("Simulated unexpected repository failure.");

        if (!_unlocks.ContainsKey(usuarioId))
            _unlocks[usuarioId] = new HashSet<int>();

        var newlyInserted = new List<int>();
        foreach (var l in levels)
        {
            if (_unlocks[usuarioId].Add(l))
                newlyInserted.Add(l);
        }

        return Task.FromResult<IReadOnlyList<int>>(newlyInserted);
    }
}
