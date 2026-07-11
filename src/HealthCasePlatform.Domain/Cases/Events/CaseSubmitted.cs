using HealthCasePlatform.Domain.Common;

namespace HealthCasePlatform.Domain.Cases.Events;

public sealed record CaseSubmitted(Guid CaseId, DateTime OccurredAtUtc) : IDomainEvent;
