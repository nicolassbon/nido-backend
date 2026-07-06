using System.Threading;
using System.Threading.Tasks;

namespace Nido.Application.Productos;

public interface IPriceComparatorService
{
    Task<ComparePricesResult> CompareAsync(string query, CancellationToken ct);
}
