namespace Nido.Application.Onboarding;

public sealed class GetPreferenciasAlimentariasHandler
{
    private readonly IOnboardingRepository _repository;

    public GetPreferenciasAlimentariasHandler(IOnboardingRepository repository)
    {
        _repository = repository;
    }

    public Task<IReadOnlyList<RestriccionCatalogoResult>> Handle(CancellationToken cancellationToken)
        => _repository.GetRestriccionesCatalogoAsync("restriccion_alimentaria", null, cancellationToken);
}
