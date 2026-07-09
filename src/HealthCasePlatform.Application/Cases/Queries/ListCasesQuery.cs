using HealthCasePlatform.Application.Cases.Models;
using HealthCasePlatform.Domain.Cases;
using HealthCasePlatform.Domain.Enums;
using Mediator;

namespace HealthCasePlatform.Application.Cases.Queries;

public sealed record ListCasesQuery(
    CaseStatus? Status,
    CasePriority? Priority,
    string? Country,
    int Page,
    int PageSize) : IQuery<PagedResult<RegulatoryCase>>;
