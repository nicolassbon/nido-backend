namespace Nido.Api.Contracts.Onboarding;

public sealed record WellnessOnboardingRequest(
    bool Skip,
    Guid? UsuarioId,
    Guid? HogarId,
    IReadOnlyList<Guid>? RestriccionIds,
    IReadOnlyList<Guid>? MetaIds);
