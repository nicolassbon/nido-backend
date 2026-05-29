namespace Nido.Application.Onboarding;

public sealed record SaveWellnessStepCommand(
    Guid UsuarioId,
    Guid HogarId,
    bool Skip,
    IReadOnlyList<Guid> RestriccionIds,
    IReadOnlyList<Guid> MetaIds,
    Guid? ClientUsuarioId,
    Guid? ClientHogarId);
