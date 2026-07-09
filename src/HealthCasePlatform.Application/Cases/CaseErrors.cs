using ErrorOr;

namespace HealthCasePlatform.Application.Cases;

internal static class CaseErrors
{
    public static readonly Error NotFound = Error.NotFound("RegulatoryCase.NotFound", "Case was not found.");
}
