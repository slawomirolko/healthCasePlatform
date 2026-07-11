using ErrorOr;

namespace HealthCasePlatform.Domain.Cases;

public static class AuditEntryErrors
{
    public static readonly Error CaseIdEmpty = Error.Validation("AuditEntry.CaseIdEmpty", "Case ID cannot be empty.");
    public static readonly Error ActorEmpty = Error.Validation("AuditEntry.ActorEmpty", "Actor cannot be empty.");
}
