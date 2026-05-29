namespace Nido.Application.Onboarding;

public sealed class GetAlergiasHandler
{
    private readonly IOnboardingRepository _repository;

    public GetAlergiasHandler(IOnboardingRepository repository)
    {
        _repository = repository;
    }

    public Task<IReadOnlyList<RestriccionCatalogoResult>> Handle(string? search, CancellationToken cancellationToken)
        => _repository.GetRestriccionesCatalogoAsync("alergia", search, cancellationToken);
}
