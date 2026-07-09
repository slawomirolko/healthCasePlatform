using HealthCasePlatform.Domain.Cases;
using Mediator;

namespace HealthCasePlatform.Application.Cases.Queries;

public sealed record GetCaseQuery(Guid Id) : IQuery<RegulatoryCase?>;
