namespace Nido.Api.Contracts.Gamificacion;

public sealed record GamificacionProgresoResponse(
    Guid UsuarioId,
    int CurrentXp,
    int CurrentLevel,
    int? NextLevel,
    int? NextThresholdXp,
    int? XpToNextLevel,
    bool HasNextLevel);
