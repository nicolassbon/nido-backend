using System;
using System.Threading;
using System.Threading.Tasks;
using Nido.Application.Payments;

namespace Nido.Application.Productos;

public sealed class ComparePricesHandler
{
    private readonly IPriceComparatorService _comparatorService;
    private readonly IEntitlementService _entitlementService;

    public ComparePricesHandler(
        IPriceComparatorService comparatorService,
        IEntitlementService entitlementService)
    {
        _comparatorService = comparatorService;
        _entitlementService = entitlementService;
    }

    public async Task<ComparePricesResult> Handle(ComparePricesQuery query, CancellationToken ct)
    {
        await _entitlementService.EnsurePremiumAsync(query.HogarId, ct);

        if (string.IsNullOrWhiteSpace(query.Query))
        {
            throw new ArgumentException("El término de búsqueda no puede estar vacío.", nameof(query));
        }

        // El comparador Go valida de forma estricta que la consulta sea completamente en minúsculas
        var searchNormalized = query.Query.Trim().ToLowerInvariant();

        return await _comparatorService.CompareAsync(searchNormalized, ct);
    }
}
