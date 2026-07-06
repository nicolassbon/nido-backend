using Microsoft.Extensions.Options;

namespace Nido.Application.Gamificacion;

public sealed class GamificationRulesService : IGamificationRulesService
{
    private readonly GamificationOptions _options;
    private readonly List<GamificationLevelOptions> _normalizedLevels;

    public GamificationRulesService(IOptions<GamificationOptions> options)
    {
        _options = options.Value;
        if (_options.XpPerCompletedTask < 0)
            throw new InvalidOperationException("Gamification XP per completed task cannot be negative.");

        // Validate and normalize levels once at construction. Configuration providers may overlay
        // indexed arrays, so the last value for a level wins before validation.
        _normalizedLevels = new List<GamificationLevelOptions>();

        foreach (var l in _options.Levels.GroupBy(l => l.Level).Select(g => g.Last()))
        {
            if (l.RequiredXp < 0)
                throw new InvalidOperationException($"Level {l.Level} has negative RequiredXp.");
            _normalizedLevels.Add(l);
        }

        _normalizedLevels.Sort((a, b) =>
        {
            var cmp = a.RequiredXp.CompareTo(b.RequiredXp);
            return cmp != 0 ? cmp : a.Level.CompareTo(b.Level);
        });
    }

    public int ComputeCurrentXp(int completedTaskCount)
    {
        if (completedTaskCount < 0) completedTaskCount = 0;
        return completedTaskCount * _options.XpPerCompletedTask;
    }

    public int? TaskXpOtorgado(bool isCompleted)
        => isCompleted ? _options.XpPerCompletedTask : null;

    public IReadOnlyList<int> ComputeEligibleLevels(int currentXp)
    {
        var eligible = new List<int>();
        foreach (var level in _normalizedLevels)
        {
            if (level.RequiredXp <= currentXp)
                eligible.Add(level.Level);
        }
        return eligible;
    }

    public NextLevelInfo? GetNextLevel(int currentXp)
    {
        foreach (var level in _normalizedLevels)
        {
            if (level.RequiredXp > currentXp)
            {
                return new NextLevelInfo(level.Level, level.RequiredXp, level.RequiredXp - currentXp);
            }
        }
        return null;
    }

    public GamificationLevelMetadata? GetLevelMetadata(int level)
    {
        var config = _normalizedLevels.FirstOrDefault(l => l.Level == level);
        if (config is null) return null;
        return new GamificationLevelMetadata(config.Level, config.Name, config.AvatarUrl);
    }
}
