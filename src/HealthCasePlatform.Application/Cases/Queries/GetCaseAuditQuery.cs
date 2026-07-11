using ErrorOr;
using HealthCasePlatform.Domain.Cases;
using Mediator;

namespace HealthCasePlatform.Application.Cases.Queries;

public sealed record GetCaseAuditQuery(Guid Id) : IQuery<ErrorOr<IReadOnlyList<AuditEntry>>>;
