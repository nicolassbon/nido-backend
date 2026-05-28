namespace Nido.Application.Onboarding;

public sealed class SaveHouseholdStepHandler
{
    private readonly IOnboardingRepository _repository;

    public SaveHouseholdStepHandler(IOnboardingRepository repository)
    {
        _repository = repository;
    }

    public async Task Handle(SaveHouseholdStepCommand command, CancellationToken cancellationToken)
    {
        OnboardingBoundaryGuard.EnsureClientIdsMatchClaims(command.UsuarioId, command.HogarId, command.ClientUsuarioId, command.ClientHogarId);

        if (!await _repository.IsUserHouseholdMemberAsync(command.UsuarioId, command.HogarId, cancellationToken))
        {
            throw new UnauthorizedAccessException("User does not belong to household.");
        }

        var isOwner = await _repository.IsUserHouseholdOwnerAsync(command.UsuarioId, command.HogarId, cancellationToken);
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
