using HealthCasePlatform.Domain.Enums;

namespace HealthCasePlatform.Application.Cases.Models;

public sealed record CaseListFilter(
    CaseStatus? Status = null,
    CasePriority? Priority = null,
    string? Country = null);
