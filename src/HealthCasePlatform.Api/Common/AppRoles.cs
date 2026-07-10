namespace HealthCasePlatform.Api.Common;

public static class AppRoles
{
    public const string CaseOfficer = "CaseOfficer";
    public const string ScientificReviewer = "ScientificReviewer";
    public const string LegalReviewer = "LegalReviewer";
    public const string TeamLeader = "TeamLeader";
    public const string Auditor = "Auditor";

    public static readonly string[] All =
    [
        CaseOfficer,
        ScientificReviewer,
        LegalReviewer,
        TeamLeader,
        Auditor
    ];
}
