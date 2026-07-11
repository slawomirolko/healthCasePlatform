namespace HealthCasePlatform.Infrastructure.Messaging.RabbitMq.Messages;

internal sealed record CaseSubmittedMessage(Guid CaseId, DateTime OccurredAtUtc);
