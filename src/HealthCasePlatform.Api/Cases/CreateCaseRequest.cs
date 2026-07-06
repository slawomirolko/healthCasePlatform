using HealthCasePlatform.Domain.Enums;

namespace HealthCasePlatform.Api.Cases;

public sealed record CreateCaseRequest(
    string Title,
    string? Description,
    Guid CaseTypeId,
    CasePriority Priority,
    string CreatedBy,
    string Country);
