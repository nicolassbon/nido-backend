namespace Nido.Api.Contracts.Gamificacion;

public sealed record GamificacionProgresoResponse(
    Guid UsuarioId,
    int CurrentXp,
    int CurrentLevel,
    string? CurrentLevelNombre,
    string? CurrentLevelAvatarUrl,
    int? NextLevel,
    string? NextLevelNombre,
    string? NextLevelAvatarUrl,
    int? NextThresholdXp,
    int? XpToNextLevel,
    bool HasNextLevel);
