namespace Nido.Application.Gamificacion;

public sealed record GamificationProgressResult(
    Guid UsuarioId,
    int CurrentXp,
    int CurrentLevel,
    int CurrentLevelThresholdXp,
    int? NextLevel,
    int? NextThresholdXp,
    int? XpToNextLevel,
    bool HasNextLevel);
