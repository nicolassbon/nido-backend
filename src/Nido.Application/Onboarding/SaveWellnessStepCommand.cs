namespace Nido.Application.Onboarding;

public sealed record RestrictionInput(string Tipo, string? Descripcion);
public sealed record HouseholdGoalInput(string Titulo, string? Descripcion);

public sealed record SaveWellnessStepCommand(
    Guid UsuarioId,
    Guid HogarId,
    bool Skip,
    IReadOnlyList<RestrictionInput> Restricciones,
    IReadOnlyList<HouseholdGoalInput> Goals,
    Guid? ClientUsuarioId,
    Guid? ClientHogarId);
