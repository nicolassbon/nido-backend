namespace Nido.Api.Contracts.Onboarding;

public sealed record WellnessRestrictionRequest(string Tipo, string? Descripcion);
public sealed record WellnessGoalRequest(string Titulo, string? Descripcion);

public sealed record WellnessOnboardingRequest(
    bool Skip,
    Guid? UsuarioId,
    Guid? HogarId,
    IReadOnlyList<WellnessRestrictionRequest>? Restricciones,
    IReadOnlyList<WellnessGoalRequest>? Goals);
