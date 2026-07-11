using ErrorOr;
using HealthCasePlatform.Domain.Common;
using HealthCasePlatform.Domain.Enums;

namespace HealthCasePlatform.Domain.Cases;

public sealed class AuditEntry : Entity
{
    public Guid CaseId { get; private set; }
    public AuditAction Action { get; private set; }
    public string Actor { get; private set; }
    public string? Detail { get; private set; }
    public DateTime OccurredAt { get; private set; }

    private AuditEntry() { }

    public static ErrorOr<AuditEntry> Create(Guid caseId, AuditAction action, string actor, string? detail = null)
    {
        if (caseId == Guid.Empty)
        {
            return AuditEntryErrors.CaseIdEmpty;
        }

        if (string.IsNullOrWhiteSpace(actor))
        {
            return AuditEntryErrors.ActorEmpty;
        }

        return new AuditEntry
        {
            Id = Guid.CreateVersion7(),
            CaseId = caseId,
            Action = action,
            Actor = actor,
            Detail = detail,
            OccurredAt = DateTime.UtcNow
        };
    }
}
