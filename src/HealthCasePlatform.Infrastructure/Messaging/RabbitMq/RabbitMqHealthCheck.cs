using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;

namespace HealthCasePlatform.Infrastructure.Messaging.RabbitMq;

internal sealed class RabbitMqHealthCheck : IHealthCheck
{
    private readonly IConnectionFactory _connectionFactory;
    private readonly ILogger<RabbitMqHealthCheck> _logger;

    public RabbitMqHealthCheck(IConnectionFactory connectionFactory, ILogger<RabbitMqHealthCheck> logger)
    {
        _connectionFactory = connectionFactory;
        _logger = logger;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            await using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);
            await using var channel = await connection.CreateChannelAsync(cancellationToken: cancellationToken);
            return HealthCheckResult.Healthy("RabbitMQ reachable");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "RabbitMQ health check failed");
            return HealthCheckResult.Unhealthy("RabbitMQ unreachable", ex);
        }
    }
}
