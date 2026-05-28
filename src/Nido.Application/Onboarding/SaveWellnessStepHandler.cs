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
            throw new UnauthorizedAccessException("User does not belong to household.");
        }

        if (!command.Skip)
        {
            await _repository.ReplaceRestrictionsAsync(command.UsuarioId, command.Restricciones, cancellationToken);
            await _repository.ReplaceGoalsAsync(command.HogarId, command.Goals, cancellationToken);
        }

        await _repository.MarkStepAsync(command.UsuarioId, command.HogarId, 4, command.Skip, cancellationToken);
    }
}
