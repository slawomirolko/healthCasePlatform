namespace HealthCasePlatform.Api.Cases;

public sealed record AuditEntryResponse(
    Guid Id,
    Guid CaseId,
    string Action,
    string Actor,
    string? Detail,
    DateTime OccurredAt);
