using System;
using System.Threading;
using System.Threading.Tasks;

namespace Nido.Application.Productos;

public sealed class ComparePricesHandler
{
    private readonly IPriceComparatorService _comparatorService;

    public ComparePricesHandler(IPriceComparatorService comparatorService)
    {
        _comparatorService = comparatorService;
    }

    public async Task<ComparePricesResult> Handle(ComparePricesQuery query, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(query.Query))
        {
            throw new ArgumentException("El término de búsqueda no puede estar vacío.", nameof(query));
        }

        // El comparador Go valida de forma estricta que la consulta sea completamente en minúsculas
        var searchNormalized = query.Query.Trim().ToLowerInvariant();

        return await _comparatorService.CompareAsync(searchNormalized, ct);
    }
}
