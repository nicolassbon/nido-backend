namespace Nido.Application.Gamificacion;

public sealed class GamificationOptions
{
    public const string SectionName = "Gamification";
    public int XpPerCompletedTask { get; set; } = 20;
    public List<GamificationLevelOptions> Levels { get; set; } = new();
}
