namespace HealthCasePlatform.Api.Cases;

public sealed record CaseStatusHistoryResponse(
    Guid Id,
    Guid CaseId,
    string FromStatus,
    string ToStatus,
    DateTime TransitionedAt);
