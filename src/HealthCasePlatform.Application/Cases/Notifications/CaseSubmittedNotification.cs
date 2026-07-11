using Mediator;

namespace HealthCasePlatform.Application.Cases.Notifications;

public sealed record CaseSubmittedNotification(Guid CaseId, DateTime OccurredAtUtc) : INotification;
