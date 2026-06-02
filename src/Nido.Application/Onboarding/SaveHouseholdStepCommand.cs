namespace Nido.Application.Onboarding;

public sealed record RepresentedMemberInput(string Nombre, string? Rol);

public sealed record SaveHouseholdStepCommand(
    Guid UsuarioId,
    Guid HogarId,
    bool Skip,
    IReadOnlyList<RepresentedMemberInput> Members,
    Guid? ClientUsuarioId,
    Guid? ClientHogarId);
