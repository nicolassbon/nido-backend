using System.Threading;
using System.Threading.Tasks;

namespace Nido.Application.Telegram.Messaging;

public interface ITelegramOutboxWakeupService
{
    Task WaitForMessageAsync(CancellationToken ct);
    void TriggerWakeup();
}
