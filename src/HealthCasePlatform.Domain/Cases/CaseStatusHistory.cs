using HealthCasePlatform.Domain.Common;
using HealthCasePlatform.Domain.Enums;

namespace HealthCasePlatform.Domain.Cases;

public sealed class CaseStatusHistory : Entity
{
    public Guid CaseId { get; private set; }
    public CaseStatus FromStatus { get; private set; }
    public CaseStatus ToStatus { get; private set; }
    public DateTime TransitionedAt { get; private set; }

    private CaseStatusHistory() { }

    internal static CaseStatusHistory Create(Guid caseId, CaseStatus fromStatus, CaseStatus toStatus, DateTime transitionedAt)
    {
        return new CaseStatusHistory
        {
            Id = Guid.CreateVersion7(),
            CaseId = caseId,
            FromStatus = fromStatus,
            ToStatus = toStatus,
            TransitionedAt = transitionedAt
        };
    }
}
