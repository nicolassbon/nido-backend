namespace Nido.Application.Recetas;

public sealed record GetNotasByRecetaQuery(Guid RecetaId, Guid HogarId);
public sealed record GetNotasByRecetaResult(IReadOnlyList<NotaItem> Items);

public sealed class GetNotasByRecetaHandler
{
    private readonly INotaRecetaRepository _repository;

    public GetNotasByRecetaHandler(INotaRecetaRepository repository)
    {
        _repository = repository;
    }

    public async Task<GetNotasByRecetaResult> Handle(GetNotasByRecetaQuery query, CancellationToken ct)
    {
        var items = await _repository.GetByRecetaAndHogarAsync(query.RecetaId, query.HogarId, ct);
        return new GetNotasByRecetaResult(items);
    }
}
