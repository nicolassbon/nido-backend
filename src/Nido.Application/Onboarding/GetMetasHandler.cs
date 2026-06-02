namespace Nido.Application.Onboarding;

public sealed class GetMetasHandler
{
    private readonly IOnboardingRepository _repository;

    public GetMetasHandler(IOnboardingRepository repository)
    {
        _repository = repository;
    }

    public Task<IReadOnlyList<MetaCatalogoResult>> Handle(CancellationToken cancellationToken)
        => _repository.GetMetasCatalogoAsync(cancellationToken);
}
