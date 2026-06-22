using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Nido.Application.Telegram.Messaging;

namespace Nido.Infrastructure.Telegram.Messaging;

public sealed class TelegramBatchingWorker : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<TelegramBatchingWorker> _logger;

    public TelegramBatchingWorker(
        IServiceProvider serviceProvider,
        ILogger<TelegramBatchingWorker> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Telegram Batching Worker is starting...");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                // Wait 30 seconds between checks
                await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);

                _logger.LogDebug("Telegram Batching Worker is checking for expired batches...");

                using var scope = _serviceProvider.CreateScope();
                var batcher = scope.ServiceProvider.GetRequiredService<ITelegramNotificationBatcher>();
                await batcher.ProcessExpiredBatchesAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred executing Telegram Batching Worker iteration.");
            }
        }

        _logger.LogInformation("Telegram Batching Worker has stopped.");
    }
}
