namespace HealthCasePlatform.Api.Cases;

public sealed record CaseListItemResponse(
    Guid Id,
    string Title,
    string Country,
    string Status,
    string Priority,
    DateTime CreatedAt);
