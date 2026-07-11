namespace HealthCasePlatform.Domain.Cases.Messaging;

public interface ICaseEventOutbox
{
    Task EnqueueCaseSubmittedAsync(Guid caseId, DateTime occurredAtUtc, CancellationToken cancellationToken);
}
