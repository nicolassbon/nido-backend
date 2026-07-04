namespace Nido.Application.Gamificacion;

public sealed record GamificationLevelMetadata(int Level, string? Name, string? AvatarUrl);

public sealed record NextLevelInfo(int Level, int ThresholdXp, int XpToNextLevel);

public interface IGamificationRulesService
{
    int ComputeCurrentXp(int completedTaskCount);
    int? TaskXpOtorgado(bool isCompleted);
    IReadOnlyList<int> ComputeEligibleLevels(int currentXp);
    NextLevelInfo? GetNextLevel(int currentXp);
    GamificationLevelMetadata? GetLevelMetadata(int level);
}
