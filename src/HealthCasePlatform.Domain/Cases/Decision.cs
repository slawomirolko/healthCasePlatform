using ErrorOr;
using HealthCasePlatform.Domain.Common;

namespace HealthCasePlatform.Domain.Cases;

public sealed class Decision : Entity
{
    public Guid CaseId { get; private set; }
    public string DecisionText { get; private set; }
    public string DecidedBy { get; private set; }
    public DateTime DecidedAt { get; private set; }
    public bool IsFinal { get; private set; }

    private Decision() { }

    public static ErrorOr<Decision> Create(Guid caseId, string decisionText, string decidedBy)
    {
        if (caseId == Guid.Empty)
        {
            return DecisionErrors.CaseIdEmpty;
        }

        if (string.IsNullOrWhiteSpace(decisionText))
        {
            return DecisionErrors.DecisionTextEmpty;
        }

        if (string.IsNullOrWhiteSpace(decidedBy))
        {
            return DecisionErrors.DecidedByEmpty;
        }

        return new Decision
        {
            Id = Guid.CreateVersion7(),
            CaseId = caseId,
            DecisionText = decisionText,
            DecidedBy = decidedBy,
            DecidedAt = DateTime.UtcNow,
            IsFinal = false
        };
    }

    public ErrorOr<Success> MarkFinal()
    {
        if (IsFinal)
        {
            return DecisionErrors.AlreadyFinal;
        }

        IsFinal = true;
        return Result.Success;
    }
}
