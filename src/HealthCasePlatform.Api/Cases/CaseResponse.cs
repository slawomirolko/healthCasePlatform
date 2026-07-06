namespace HealthCasePlatform.Api.Cases;

public sealed record CaseResponse(
    Guid Id,
    string Title,
    string Description,
    Guid CaseTypeId,
    string Status,
    string Priority,
    string CreatedBy,
    DateTime CreatedAt,
    DateTime? UpdatedAt);
