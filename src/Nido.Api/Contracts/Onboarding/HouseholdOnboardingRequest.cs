namespace Nido.Api.Contracts.Onboarding;

public sealed record HouseholdMemberRequest(string Nombre, string? Rol);

public sealed record HouseholdOnboardingRequest(
    bool Skip,
    Guid? UsuarioId,
    Guid? HogarId,
    IReadOnlyList<HouseholdMemberRequest>? Members);
