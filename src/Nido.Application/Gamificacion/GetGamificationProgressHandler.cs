using Nido.Application.Common.Security;

namespace Nido.Application.Gamificacion;

public sealed class GetGamificationProgressHandler
{
    private readonly IGamificationUnlockMaterializer _materializer;
    private readonly IGamificationRepository _repository;
    private readonly IGamificationRulesService _rules;

    public GetGamificationProgressHandler(
        IGamificationUnlockMaterializer materializer,
        IGamificationRepository repository,
        IGamificationRulesService rules)
    {
        _materializer = materializer;
        _repository = repository;
        _rules = rules;
    }

    public async Task<GamificationProgressResult> Handle(Guid usuarioId, CancellationToken ct)
    {
        // Materialize any missing eligible unlocks first
        var newlyInserted = await _materializer.MaterializeEligibleUnlocksAsync(usuarioId, ct);

        // Count completed tasks and compute XP
        var completedCount = await _repository.CountCurrentlyCompletedTasksAsync(usuarioId, ct);
        var currentXp = _rules.ComputeCurrentXp(completedCount);

        // Get all unlocked levels (including newly materialized ones)
        var unlockedLevels = await _repository.GetUnlockedLevelsAsync(usuarioId, ct);

        // Merge with newly inserted to ensure response includes fresh rows
        var allUnlocked = new HashSet<int>(unlockedLevels);
        foreach (var l in newlyInserted)
            allUnlocked.Add(l);

        var currentLevel = allUnlocked.Count > 0 ? allUnlocked.Max() : 0;

        var nextLevel = _rules.GetNextLevel(currentXp);
        var hasNextLevel = nextLevel is not null;

        return new GamificationProgressResult(
            usuarioId,
            currentXp,
            currentLevel,
            _rules.GetLevelThreshold(currentLevel),
            nextLevel?.Level,
            nextLevel?.ThresholdXp,
            nextLevel?.XpToNextLevel,
            hasNextLevel);
    }
}
