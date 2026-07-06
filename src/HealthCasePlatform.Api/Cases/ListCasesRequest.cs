using HealthCasePlatform.Domain.Enums;

namespace HealthCasePlatform.Api.Cases;

public sealed record ListCasesRequest(
    int Page = 1,
    int PageSize = 20,
    CaseStatus? Status = null,
    CasePriority? Priority = null,
    string? Country = null);
