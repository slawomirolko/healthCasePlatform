namespace HealthCasePlatform.Infrastructure.Messaging.RabbitMq;

public sealed record RabbitMqSettings
{
    public const string SectionName = "RabbitMq";
    public string Host { get; init; } = string.Empty;
    public int Port { get; init; }
    public string UserName { get; init; } = string.Empty;
    public string Password { get; init; } = string.Empty;
    public string VirtualHost { get; init; } = string.Empty;
    public string CaseSubmittedQueue { get; init; } = string.Empty;
    public int DispatcherIntervalSeconds { get; init; }
}
