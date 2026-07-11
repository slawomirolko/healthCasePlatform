using System.Text.Json;
using HealthCasePlatform.Infrastructure.Messaging.RabbitMq.Messages;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;

namespace HealthCasePlatform.Infrastructure.Messaging.RabbitMq;

internal sealed class RabbitMqCaseSubmittedPublisher
{
    private const string ExchangeName = "case-events";
    private const string RoutingKey = "case.submitted";

    private readonly RabbitMqSettings _settings;
    private readonly IConnectionFactory _connectionFactory;
    private readonly ILogger<RabbitMqCaseSubmittedPublisher> _logger;
    private readonly SemaphoreSlim _gate = new(1, 1);

    private IConnection? _connection;
    private IChannel? _channel;

    public RabbitMqCaseSubmittedPublisher(
        IOptions<RabbitMqSettings> settings,
        IConnectionFactory connectionFactory,
        ILogger<RabbitMqCaseSubmittedPublisher> logger)
    {
        _settings = settings.Value;
        _connectionFactory = connectionFactory;
        _logger = logger;
    }

    public async Task PublishAsync(CaseSubmittedMessage message, CancellationToken cancellationToken)
    {
        var channel = await GetChannelAsync(cancellationToken);

        var body = JsonSerializer.SerializeToUtf8Bytes(message);
        var props = new BasicProperties { Persistent = true, ContentType = "application/json" };

        try
        {
            await channel.BasicPublishAsync(ExchangeName, RoutingKey, true, props, body, cancellationToken);
            _logger.LogInformation("Published CaseSubmitted message for case {CaseId}", message.CaseId);
        }
        catch
        {
            await InvalidateAsync();
            throw;
        }
    }

    private async Task<IChannel> GetChannelAsync(CancellationToken cancellationToken)
    {
        await _gate.WaitAsync(cancellationToken);
        try
        {
            if (_channel is { IsOpen: true })
            {
                return _channel;
            }

            _connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);
            _channel = await _connection.CreateChannelAsync(cancellationToken: cancellationToken);

            await _channel.ExchangeDeclareAsync(ExchangeName, ExchangeType.Direct, durable: true, cancellationToken: cancellationToken);
            await _channel.QueueDeclareAsync(_settings.CaseSubmittedQueue, durable: true, exclusive: false, autoDelete: false, cancellationToken: cancellationToken);
            await _channel.QueueBindAsync(_settings.CaseSubmittedQueue, ExchangeName, RoutingKey, cancellationToken: cancellationToken);

            return _channel;
        }
        finally
        {
            _gate.Release();
        }
    }

    private async Task InvalidateAsync()
    {
        await _gate.WaitAsync();
        try
        {
            if (_channel is { IsOpen: true })
            {
                await _channel.DisposeAsync();
            }

            _channel = null;

            if (_connection is { IsOpen: true })
            {
                await _connection.DisposeAsync();
            }

            _connection = null;
        }
        finally
        {
            _gate.Release();
        }
    }
}
