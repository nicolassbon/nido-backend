using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Nido.Application.Notificaciones;
using Nido.Infrastructure.Persistence;

namespace Nido.Infrastructure.Notificaciones;

public sealed class AlertaDiariaWorker : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<AlertaDiariaWorker> _logger;
    private readonly TimeProvider _timeProvider;

    public AlertaDiariaWorker(
        IServiceProvider serviceProvider,
        ILogger<AlertaDiariaWorker> logger,
        TimeProvider? timeProvider = null)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _timeProvider = timeProvider ?? TimeProvider.System;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Daily Alert Background Worker is starting...");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var now = _timeProvider.GetUtcNow().DateTime;
                
                // Target execution time is 11:00 AM UTC (08:00 AM UTC-3, local time for Argentina)
                var targetToday = new DateTime(now.Year, now.Month, now.Day, 11, 0, 0, DateTimeKind.Utc);
                DateTime targetTime;
                
                if (now < targetToday)
                {
                    targetTime = targetToday;
                }
                else
                {
                    targetTime = targetToday.AddDays(1);
                }

                var delay = targetTime - now;
                _logger.LogInformation("Next daily alert check scheduled at {TargetTime} UTC (in {DelayHours:F1} hours).", targetTime, delay.TotalHours);

                await Task.Delay(delay, stoppingToken);

                _logger.LogInformation("Running daily alert evaluation for all active users...");
                await EvaluarAlertasDeUsuariosAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred during daily alert background worker iteration. Retrying in 1 hour...");
                try
                {
                    await Task.Delay(TimeSpan.FromHours(1), stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
            }
        }

        _logger.LogInformation("Daily Alert Background Worker has stopped.");
    }

    private async Task EvaluarAlertasDeUsuariosAsync(CancellationToken ct)
    {
        using var scope = _serviceProvider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<NidoDbContext>();
        var notifRepository = scope.ServiceProvider.GetRequiredService<INotificacionesRepository>();

        // Load all users to evaluate their alerts
        var usuarioIds = await db.Usuarios
            .Select(u => u.Id)
            .ToListAsync(ct);

        _logger.LogInformation("Found {Count} users to process for daily alerts.", usuarioIds.Count);

        foreach (var usuarioId in usuarioIds)
        {
            try
            {
                // This will check overdue tasks, low stock, expired/nearly-expired products,
                // and insert relevant Notificacione entries into the DB. 
                // The SaveChangesAsync hook in NidoDbContext will automatically enqueue Telegram/Web Push notifications.
                await notifRepository.GetByUsuarioAsync(usuarioId, ct);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing notifications for user {UsuarioId}", usuarioId);
            }
        }
    }
}
