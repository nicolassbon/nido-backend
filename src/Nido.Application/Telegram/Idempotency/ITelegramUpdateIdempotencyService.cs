using System.Threading;
using System.Threading.Tasks;

namespace Nido.Application.Telegram.Idempotency;

public interface ITelegramUpdateIdempotencyService
{
    Task<bool> IsAlreadyProcessedAsync(long updateId, CancellationToken ct);

    Task<bool> TryReserveAsync(long updateId, string? updateHash, CancellationToken ct);

    Task ReleaseReservationAsync(long updateId, CancellationToken ct);
}
