using Nido.Application.Common.Security;
using Nido.Application.Onboarding.Exceptions;

namespace Nido.Application.Onboarding;

public sealed class SaveWellnessStepHandler
{
    private readonly IOnboardingRepository _repository;
    private readonly IHouseholdMembershipService _membershipService;

    public SaveWellnessStepHandler(IOnboardingRepository repository, IHouseholdMembershipService membershipService)
    {
        _repository = repository;
        _membershipService = membershipService;
    }

    public async Task Handle(SaveWellnessStepCommand command, CancellationToken cancellationToken)
    {
        OnboardingBoundaryGuard.EnsureClientIdsMatchClaims(command.UsuarioId, command.HogarId, command.ClientUsuarioId, command.ClientHogarId);

        await _membershipService.EnsureMemberAsync(
            command.UsuarioId,
            command.HogarId,
            static () => new HouseholdAccessDeniedException(),
            cancellationToken);

        if (!command.Skip)
        {
            await _repository.ReplaceUserRestriccionesAsync(command.UsuarioId, command.RestriccionIds, cancellationToken);
            await _repository.ReplaceHogarMetasAsync(command.HogarId, command.MetaIds, cancellationToken);
        }

        await _repository.MarkStepAsync(command.UsuarioId, command.HogarId, 4, command.Skip, cancellationToken);
    }
}
