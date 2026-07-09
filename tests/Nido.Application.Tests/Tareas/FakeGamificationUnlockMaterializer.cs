using Nido.Application.Gamificacion;
using Nido.Application.Tareas;

namespace Nido.Application.Tests.Tareas;

/// <summary>
/// Fake for IGamificationUnlockMaterializer that tracks whether
/// MaterializeEligibleUnlocksAsync was called and for whom.
/// </summary>
public sealed class FakeGamificationUnlockMaterializer : IGamificationUnlockMaterializer
{
    public List<Guid> MaterializationCalls { get; } = new();
    public IReadOnlyList<int> ReturnLevels { get; set; } = Array.Empty<int>();

    public Task<IReadOnlyList<int>> MaterializeEligibleUnlocksAsync(Guid usuarioId, CancellationToken ct)
    {
        MaterializationCalls.Add(usuarioId);
        return Task.FromResult(ReturnLevels);
    }
}
