using Nido.Application.Common.Security;
using Nido.Application.Onboarding.Exceptions;

namespace Nido.Application.Onboarding;

public sealed class SaveHouseholdStepHandler
{
    private readonly IOnboardingRepository _repository;
    private readonly IHogarMembershipRepository _membershipRepository;
    private readonly IHouseholdMembershipService _membershipService;

    public SaveHouseholdStepHandler(
        IOnboardingRepository repository,
        IHogarMembershipRepository membershipRepository,
        IHouseholdMembershipService membershipService)
    {
        _repository = repository;
        _membershipRepository = membershipRepository;
        _membershipService = membershipService;
    }

    public async Task Handle(SaveHouseholdStepCommand command, CancellationToken cancellationToken)
    {
        OnboardingBoundaryGuard.EnsureClientIdsMatchClaims(command.UsuarioId, command.HogarId, command.ClientUsuarioId, command.ClientHogarId);

        await _membershipService.EnsureMemberAsync(
            command.UsuarioId,
            command.HogarId,
            static () => new HouseholdAccessDeniedException(),
            cancellationToken);

        var isOwner = await _membershipRepository.IsOwnerAsync(command.UsuarioId, command.HogarId, cancellationToken);
        if (!isOwner)
        {
            await _repository.MarkStepAsync(command.UsuarioId, command.HogarId, 2, true, cancellationToken);
            return;
        }

        if (!command.Skip)
        {
            await _repository.ReplaceRepresentedMembersAsync(command.HogarId, command.Members, cancellationToken);
        }

        await _repository.MarkStepAsync(command.UsuarioId, command.HogarId, 2, command.Skip, cancellationToken);
    }
}
