namespace Nido.Application.Onboarding;

public sealed record EquipmentInput(string Nombre, string? Tipo, string? Estado);

public sealed record SaveEquipmentStepCommand(
    Guid UsuarioId,
    Guid HogarId,
    bool Skip,
    IReadOnlyList<EquipmentInput> Equipments,
    Guid? ClientUsuarioId,
    Guid? ClientHogarId);
