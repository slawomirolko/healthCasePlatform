using HealthCasePlatform.Infrastructure.Messaging.RabbitMq;
using RabbitMQ.Client;
using Testcontainers.RabbitMq;

namespace HealthCasePlatform.Infrastructure.Tests.Integration.Messaging;

public sealed class RabbitMqFixture : IAsyncLifetime
{
    private readonly RabbitMqContainer _container = new RabbitMqBuilder().Build();

    public Uri ConnectionUri => new(_container.GetConnectionString());

    public RabbitMqSettings ToSettings(string queueName)
    {
        var uri = ConnectionUri;
        var userInfo = uri.UserInfo.Split(':');
        return new RabbitMqSettings
        {
            Host = uri.Host,
            Port = uri.Port,
            UserName = userInfo.Length > 0 ? userInfo[0] : string.Empty,
            Password = userInfo.Length > 1 ? userInfo[1] : string.Empty,
            VirtualHost = "/",
            CaseSubmittedQueue = queueName,
            DispatcherIntervalSeconds = 1
        };
    }

    public IConnectionFactory CreateConnectionFactory() => new ConnectionFactory { Uri = ConnectionUri };

    public Task InitializeAsync() => _container.StartAsync();

    public Task DisposeAsync() => _container.DisposeAsync().AsTask();
}
