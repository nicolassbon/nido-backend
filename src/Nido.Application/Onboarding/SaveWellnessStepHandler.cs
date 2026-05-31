using Nido.Application.Onboarding.Exceptions;

namespace Nido.Application.Onboarding;

public sealed class SaveWellnessStepHandler
{
    private readonly IOnboardingRepository _repository;

    public SaveWellnessStepHandler(IOnboardingRepository repository)
    {
        _repository = repository;
    }

    public async Task Handle(SaveWellnessStepCommand command, CancellationToken cancellationToken)
    {
        OnboardingBoundaryGuard.EnsureClientIdsMatchClaims(command.UsuarioId, command.HogarId, command.ClientUsuarioId, command.ClientHogarId);

        if (!await _repository.IsUserHouseholdMemberAsync(command.UsuarioId, command.HogarId, cancellationToken))
        {
            throw new HouseholdAccessDeniedException();
        }

        if (!command.Skip)
        {
            await _repository.ReplaceUserRestriccionesAsync(command.UsuarioId, command.RestriccionIds, cancellationToken);
            await _repository.ReplaceHogarMetasAsync(command.HogarId, command.MetaIds, cancellationToken);
        }

        await _repository.MarkStepAsync(command.UsuarioId, command.HogarId, 4, command.Skip, cancellationToken);
    }
}
