using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Nido.Application.Gamificacion;

public sealed class GamificationUnlockMaterializer : IGamificationUnlockMaterializer
{
    private readonly IGamificationRulesService _rules;
    private readonly IGamificationRepository _repository;
    private readonly ILogger<GamificationUnlockMaterializer> _logger;

    public GamificationUnlockMaterializer(
        IGamificationRulesService rules,
        IGamificationRepository repository,
        ILogger<GamificationUnlockMaterializer>? logger = null)
    {
        _rules = rules;
        _repository = repository;
        _logger = logger ?? NullLogger<GamificationUnlockMaterializer>.Instance;
    }

    public async Task<IReadOnlyList<int>> MaterializeEligibleUnlocksAsync(Guid usuarioId, CancellationToken ct)
    {
        // 1. Count currently completed tasks
        var completedCount = await _repository.CountCurrentlyCompletedTasksAsync(usuarioId, ct);

        // 2. Compute current XP
        var currentXp = _rules.ComputeCurrentXp(completedCount);

        // 3. Load existing unlocked levels
        var existingLevels = await _repository.GetUnlockedLevelsAsync(usuarioId, ct);
        var existingSet = new HashSet<int>(existingLevels);

        // 4. Compute every configured level eligible at current XP
        var allEligible = _rules.ComputeEligibleLevels(currentXp);

        // 5. Filter to only levels not yet unlocked
        var missing = allEligible.Where(l => !existingSet.Contains(l)).ToList();

        if (missing.Count == 0)
            return Array.Empty<int>();

        // 6. Insert missing levels. The repository owns unique-conflict-as-no-op handling;
        // unexpected persistence failures must propagate instead of being hidden.
        var newlyInserted = await _repository.InsertMissingUnlocksAsync(
            usuarioId, missing, DateTime.UtcNow, ct);

        // 7. Log newly inserted levels as evolution signals
        foreach (var level in newlyInserted)
        {
            _logger.LogInformation(
                "Level {Level} unlocked for user {UsuarioId} at XP {CurrentXp}.",
                level, usuarioId, currentXp);
        }

        return newlyInserted;
    }
}
