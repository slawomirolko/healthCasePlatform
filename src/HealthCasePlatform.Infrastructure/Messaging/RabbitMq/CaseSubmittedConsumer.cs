using System.Text.Json;
using HealthCasePlatform.Domain.Cases;
using HealthCasePlatform.Domain.Enums;
using HealthCasePlatform.Infrastructure.Messaging.RabbitMq.Messages;
using HealthCasePlatform.Infrastructure.Persistence;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace HealthCasePlatform.Infrastructure.Messaging.RabbitMq;

internal sealed class CaseSubmittedConsumer : BackgroundService
{
    private readonly RabbitMqSettings _settings;
    private readonly IConnectionFactory _connectionFactory;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<CaseSubmittedConsumer> _logger;

    public CaseSubmittedConsumer(
        IOptions<RabbitMqSettings> settings,
        IConnectionFactory connectionFactory,
        IServiceScopeFactory scopeFactory,
        ILogger<CaseSubmittedConsumer> logger)
    {
        _settings = settings.Value;
        _connectionFactory = connectionFactory;
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            IConnection? connection = null;
            IChannel? channel = null;

            try
            {
                connection = await _connectionFactory.CreateConnectionAsync(stoppingToken);
                channel = await connection.CreateChannelAsync(cancellationToken: stoppingToken);

                await channel.QueueDeclareAsync(
                    _settings.CaseSubmittedQueue, durable: true, exclusive: false, autoDelete: false, cancellationToken: stoppingToken);
                await channel.BasicQosAsync(0, prefetchCount: 1, global: false, cancellationToken: stoppingToken);

                var consumer = new AsyncEventingBasicConsumer(channel);
                consumer.ReceivedAsync += (_, ea) => HandleMessageAsync(channel, ea, stoppingToken);
                await channel.BasicConsumeAsync(_settings.CaseSubmittedQueue, autoAck: false, consumer);

                _logger.LogInformation("CaseSubmittedConsumer started on queue {Queue}", _settings.CaseSubmittedQueue);

                await WaitForShutdownAsync(connection, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "CaseSubmittedConsumer connection failed; retrying in 5s");
                try
                {
                    await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
            }
            finally
            {
                await DisposeAsyncSilently(channel);
                await DisposeAsyncSilently(connection);
            }
        }
    }

    private static async Task WaitForShutdownAsync(IConnection connection, CancellationToken stoppingToken)
    {
        var tcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        using var registration = stoppingToken.Register(() => tcs.TrySetResult());
        connection.ConnectionShutdownAsync += (_, _) =>
        {
            tcs.TrySetResult();
            return Task.CompletedTask;
        };

        await tcs.Task;
    }

    private async Task HandleMessageAsync(IChannel channel, BasicDeliverEventArgs ea, CancellationToken stoppingToken)
    {
        try
        {
            var message = JsonSerializer.Deserialize<CaseSubmittedMessage>(ea.Body.Span);
            if (message is null)
            {
                await channel.BasicAckAsync(ea.DeliveryTag, multiple: false, stoppingToken);
                return;
            }

            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var writer = scope.ServiceProvider.GetRequiredService<INotificationWriter>();

            var notification = Notification.Create(message.CaseId, NotificationType.CaseSubmitted);
            await writer.WriteAsync(notification.Value, stoppingToken);
            await db.SaveChangesAsync(stoppingToken);

            await channel.BasicAckAsync(ea.DeliveryTag, multiple: false, stoppingToken);
            _logger.LogInformation("Consumed CaseSubmitted message for case {CaseId}", message.CaseId);
        }
        catch (DbUpdateException ex) when (IsDuplicateKey(ex))
        {
            _logger.LogWarning("Duplicate CaseSubmitted notification redelivery; acking idempotent row");
            await channel.BasicAckAsync(ea.DeliveryTag, multiple: false, stoppingToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to process CaseSubmitted message; nacking for requeue");
            try
            {
                await channel.BasicNackAsync(ea.DeliveryTag, multiple: false, requeue: true, stoppingToken);
            }
            catch (Exception nackEx)
            {
                _logger.LogWarning(nackEx, "Failed to nack CaseSubmitted message");
            }
        }
    }

    private static bool IsDuplicateKey(DbUpdateException ex) =>
        ex.InnerException is SqlException sqlEx && (sqlEx.Number == 2627 || sqlEx.Number == 2601);

    private static async Task DisposeAsyncSilently(IAsyncDisposable? disposable)
    {
        if (disposable is null)
        {
            return;
        }

        try
        {
            await disposable.DisposeAsync();
        }
        catch
        {
        }
    }
}
