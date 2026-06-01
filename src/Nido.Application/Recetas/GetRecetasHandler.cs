namespace Nido.Application.Recetas;

public sealed class GetRecetasHandler
{
    private readonly IRecetaRepository _repository;

    public GetRecetasHandler(IRecetaRepository repository)
    {
        _repository = repository;
    }

    public Task<IReadOnlyList<RecetaResult>> Handle(CancellationToken ct)
    {
        return _repository.GetAllAsync(ct);
    }
}
