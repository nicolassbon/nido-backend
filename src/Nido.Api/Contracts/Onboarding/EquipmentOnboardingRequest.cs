namespace Nido.Api.Contracts.Onboarding;

public sealed record OnboardingEquipmentRequest(Guid? CatalogoId, string Nombre, string? Tipo, string? Estado);

public sealed record EquipmentOnboardingRequest(
    bool Skip,
    Guid? UsuarioId,
    Guid? HogarId,
    IReadOnlyList<OnboardingEquipmentRequest>? Equipments);
