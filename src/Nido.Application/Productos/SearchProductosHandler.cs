namespace Nido.Application.Productos;

public sealed class SearchProductosHandler
{
    private readonly IProductoRepository _repository;

    public SearchProductosHandler(IProductoRepository repository)
    {
        _repository = repository;
    }

    public async Task<IEnumerable<SearchProductosResult>> Handle(
        SearchProductosQuery query,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(query.Q) || query.Q.Trim().Length < 2)
            return Array.Empty<SearchProductosResult>();

        return await _repository.SearchByNombreAsync(query.Q.Trim(), ct);
    }
}
