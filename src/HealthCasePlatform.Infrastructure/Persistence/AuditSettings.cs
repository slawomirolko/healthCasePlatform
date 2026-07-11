namespace HealthCasePlatform.Infrastructure.Persistence;

public sealed record AuditSettings
{
    public const string SectionName = "Audit";
    public string Provider { get; init; } = "SqlServer";
}
