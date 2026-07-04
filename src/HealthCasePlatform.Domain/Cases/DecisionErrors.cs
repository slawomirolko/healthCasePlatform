using ErrorOr;

namespace HealthCasePlatform.Domain.Cases;

public static class DecisionErrors
{
    public static readonly Error CaseIdEmpty = Error.Validation("Decision.CaseIdEmpty", "Case ID cannot be empty.");
    public static readonly Error DecisionTextEmpty = Error.Validation("Decision.DecisionTextEmpty", "Decision text cannot be empty.");
    public static readonly Error DecidedByEmpty = Error.Validation("Decision.DecidedByEmpty", "Decided by cannot be empty.");
    public static readonly Error AlreadyFinal = Error.Conflict("Decision.AlreadyFinal", "Decision is already final.");
}
