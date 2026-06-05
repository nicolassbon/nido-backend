namespace Nido.Api.Contracts.Onboarding;

public sealed record WellnessOnboardingResponse(
    IReadOnlyList<Guid> RestriccionIds,
    IReadOnlyList<Guid> MetaIds);
