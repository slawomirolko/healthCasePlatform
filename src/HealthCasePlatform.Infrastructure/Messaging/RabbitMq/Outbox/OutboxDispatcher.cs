using System.Text.Json;
using HealthCasePlatform.Domain.Cases.Messaging;
using HealthCasePlatform.Infrastructure.Messaging.RabbitMq.Messages;
using HealthCasePlatform.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace HealthCasePlatform.Infrastructure.Messaging.RabbitMq.Outbox;

internal sealed class OutboxDispatcher : BackgroundService
{
    private const int BatchSize = 50;

    private readonly RabbitMqSettings _settings;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly RabbitMqCaseSubmittedPublisher _publisher;
    private readonly ILogger<OutboxDispatcher> _logger;

    public OutboxDispatcher(
        IOptions<RabbitMqSettings> settings,
        IServiceScopeFactory scopeFactory,
        RabbitMqCaseSubmittedPublisher publisher,
        ILogger<OutboxDispatcher> logger)
    {
        _settings = settings.Value;
        _scopeFactory = scopeFactory;
        _publisher = publisher;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var interval = TimeSpan.FromSeconds(Math.Max(1, _settings.DispatcherIntervalSeconds));

        using var timer = new PeriodicTimer(interval);

        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                await DispatchBatchAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "OutboxDispatcher tick failed; will retry next tick");
            }
        }
    }

    private async Task DispatchBatchAsync(CancellationToken stoppingToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var pending = await db.OutboxMessages
            .Where(o => o.ProcessedAtUtc == null)
            .OrderBy(o => o.OccurredAtUtc)
            .Take(BatchSize)
            .ToListAsync(stoppingToken);

        if (pending.Count == 0)
        {
            return;
        }

        foreach (var entry in pending)
        {
            var message = JsonSerializer.Deserialize<CaseSubmittedMessage>(entry.Payload);
            if (message is null)
            {
                entry.Attempts++;
                entry.LastError = "Failed to deserialize payload";
                continue;
            }

            try
            {
                await _publisher.PublishAsync(message, stoppingToken);
                entry.ProcessedAtUtc = DateTime.UtcNow;
            }
            catch (Exception ex)
            {
                entry.Attempts++;
                entry.LastError = ex.Message.Length > 2000 ? ex.Message[..2000] : ex.Message;
                _logger.LogWarning(ex, "Failed to publish outbox entry {OutboxId}; will retry next tick", entry.Id);
            }
        }

        await db.SaveChangesAsync(stoppingToken);
    }
}
