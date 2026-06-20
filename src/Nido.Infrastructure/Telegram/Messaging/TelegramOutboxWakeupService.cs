using System;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Nido.Application.Telegram;
using Nido.Application.Telegram.Messaging;
using Npgsql;

namespace Nido.Infrastructure.Telegram.Messaging;

public sealed class TelegramOutboxWakeupService : ITelegramOutboxWakeupService, IHostedService, IDisposable
{
    private readonly string? _connectionString;
    private readonly TelegramOptions _options;
    private readonly ILogger<TelegramOutboxWakeupService> _logger;
    private readonly Channel<bool> _channel;
    private readonly CancellationTokenSource _cts;
    private Task? _listenTask;
    private Task? _pollTask;

    public TelegramOutboxWakeupService(
        IConfiguration configuration,
        TelegramOptions options,
        ILogger<TelegramOutboxWakeupService> logger)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? configuration.GetConnectionString("Nido");
        _options = options;
        _logger = logger;
        _channel = Channel.CreateUnbounded<bool>(new UnboundedChannelOptions
        {
            SingleReader = true,
            SingleWriter = false
        });
        _cts = new CancellationTokenSource();
    }

    public void TriggerWakeup()
    {
        _channel.Writer.TryWrite(true);
    }

    public async Task WaitForMessageAsync(CancellationToken ct)
    {
        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(ct, _cts.Token);
        await _channel.Reader.ReadAsync(linkedCts.Token);
        
        while (_channel.Reader.TryRead(out _)) { }
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Starting Telegram Outbox Wakeup Service...");
        
        if (!string.IsNullOrWhiteSpace(_connectionString) && _connectionString.Contains("Host=", StringComparison.OrdinalIgnoreCase))
        {
            _listenTask = Task.Run(() => ListenToPostgresNotificationsAsync(_cts.Token));
        }
        else
        {
            _logger.LogWarning("Postgres ConnectionString not found or invalid. LISTEN/NOTIFY wakeup disabled.");
        }

        _pollTask = Task.Run(() => PollingFallbackAsync(_cts.Token));

        return Task.CompletedTask;
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Stopping Telegram Outbox Wakeup Service...");
        _cts.Cancel();

        if (_listenTask != null)
        {
            try
            {
                await Task.WhenAny(_listenTask, Task.Delay(Timeout.Infinite, cancellationToken));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error stopping LISTEN task.");
            }
        }

        if (_pollTask != null)
        {
            try
            {
                await Task.WhenAny(_pollTask, Task.Delay(Timeout.Infinite, cancellationToken));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error stopping polling task.");
            }
        }
    }

    private async Task ListenToPostgresNotificationsAsync(CancellationToken ct)
    {
        const string channelName = "telegram_outbox_channel";
        var delay = 2000;

        while (!ct.IsCancellationRequested)
        {
            try
            {
                await using var conn = new NpgsqlConnection(_connectionString);
                await conn.OpenAsync(ct);

                _logger.LogInformation("Listening to PostgreSQL channel '{ChannelName}'...", channelName);
                
                await using (var cmd = new NpgsqlCommand($"LISTEN {channelName}", conn))
                {
                    await cmd.ExecuteNonQueryAsync(ct);
                }

                conn.Notification += (sender, args) =>
                {
                    _logger.LogDebug("Received database NOTIFY signal on channel '{ChannelName}'", channelName);
                    TriggerWakeup();
                };

                delay = 2000;

                while (!ct.IsCancellationRequested)
                {
                    await conn.WaitAsync(ct);
                }
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error in PostgreSQL LISTEN loop. Retrying in {Delay}ms...", delay);
                try
                {
                    await Task.Delay(delay, ct);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                delay = Math.Min(delay * 2, 30000);
            }
        }
    }

    private async Task PollingFallbackAsync(CancellationToken ct)
    {
        var intervalSeconds = _options.OutboxPollIntervalSeconds;
        if (intervalSeconds <= 0)
        {
            intervalSeconds = 30;
        }

        var interval = TimeSpan.FromSeconds(intervalSeconds);
        _logger.LogInformation("Starting outbox fallback polling with interval of {Interval}...", interval);

        using var timer = new PeriodicTimer(interval);
        
        while (!ct.IsCancellationRequested)
        {
            try
            {
                await timer.WaitForNextTickAsync(ct);
                _logger.LogDebug("Outbox fallback polling timer ticked.");
                TriggerWakeup();
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in fallback polling loop.");
            }
        }
    }

    public void Dispose()
    {
        _cts.Dispose();
    }
}
