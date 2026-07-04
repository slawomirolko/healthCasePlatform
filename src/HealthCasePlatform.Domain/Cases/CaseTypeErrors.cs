using ErrorOr;

namespace HealthCasePlatform.Domain.Cases;

public static class CaseTypeErrors
{
    public static readonly Error NameEmpty = Error.Validation("CaseType.NameEmpty", "Case type name cannot be empty.");
}
