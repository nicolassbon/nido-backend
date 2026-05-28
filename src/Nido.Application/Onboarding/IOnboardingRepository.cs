namespace Nido.Application.Onboarding;

public interface IOnboardingRepository
{
    Task<bool> IsUserHouseholdMemberAsync(Guid usuarioId, Guid hogarId, CancellationToken cancellationToken);
    Task<bool> IsUserHouseholdOwnerAsync(Guid usuarioId, Guid hogarId, CancellationToken cancellationToken);
    Task ReplaceRepresentedMembersAsync(Guid hogarId, IReadOnlyList<RepresentedMemberInput> members, CancellationToken cancellationToken);
    Task ReplaceHouseholdEquipmentAsync(Guid hogarId, IReadOnlyList<EquipmentInput> equipments, CancellationToken cancellationToken);
    Task ReplaceRestrictionsAsync(Guid usuarioId, IReadOnlyList<RestrictionInput> restrictions, CancellationToken cancellationToken);
    Task ReplaceGoalsAsync(Guid hogarId, IReadOnlyList<HouseholdGoalInput> goals, CancellationToken cancellationToken);
    Task MarkStepAsync(Guid usuarioId, Guid hogarId, int stepNumber, bool skipped, CancellationToken cancellationToken);
}
