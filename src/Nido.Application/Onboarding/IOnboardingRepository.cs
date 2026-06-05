namespace Nido.Application.Onboarding;

public interface IOnboardingRepository
{
    Task<bool> IsUserHouseholdMemberAsync(Guid usuarioId, Guid hogarId, CancellationToken cancellationToken);
    Task<bool> IsUserHouseholdOwnerAsync(Guid usuarioId, Guid hogarId, CancellationToken cancellationToken);
    Task ReplaceRepresentedMembersAsync(Guid hogarId, IReadOnlyList<RepresentedMemberInput> members, CancellationToken cancellationToken);
    Task ReplaceHouseholdEquipmentAsync(Guid hogarId, IReadOnlyList<EquipmentInput> equipments, CancellationToken cancellationToken);
    Task<IReadOnlyList<RestriccionCatalogoResult>> GetRestriccionesCatalogoAsync(string? tipo, string? search, CancellationToken cancellationToken);
    Task<IReadOnlyList<MetaCatalogoResult>> GetMetasCatalogoAsync(CancellationToken cancellationToken);
    Task ReplaceUserRestriccionesAsync(Guid usuarioId, IReadOnlyList<Guid> restriccionIds, CancellationToken cancellationToken);
    Task ReplaceHogarMetasAsync(Guid hogarId, IReadOnlyList<Guid> metaIds, CancellationToken cancellationToken);
    Task MarkStepAsync(Guid usuarioId, Guid hogarId, int stepNumber, bool skipped, CancellationToken cancellationToken);
    Task<IReadOnlyList<Guid>> GetUserRestriccionesAsync(Guid usuarioId, CancellationToken cancellationToken);
    Task<IReadOnlyList<Guid>> GetHogarMetasAsync(Guid hogarId, CancellationToken cancellationToken);
}
