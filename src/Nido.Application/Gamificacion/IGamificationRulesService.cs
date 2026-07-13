namespace Nido.Application.Gamificacion;

public sealed record NextLevelInfo(int Level, int ThresholdXp, int XpToNextLevel);

public interface IGamificationRulesService
{
    int ComputeCurrentXp(int completedTaskCount);
    int? TaskXpOtorgado(bool isCompleted);
    IReadOnlyList<int> ComputeEligibleLevels(int currentXp);
    int GetLevelThreshold(int level);
    NextLevelInfo? GetNextLevel(int currentXp);
}
