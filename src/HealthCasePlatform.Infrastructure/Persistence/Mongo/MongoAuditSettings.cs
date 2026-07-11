namespace HealthCasePlatform.Infrastructure.Persistence.Mongo;

public sealed record MongoAuditSettings
{
    public const string SectionName = "MongoAudit";
    public string ConnectionString { get; init; } = string.Empty;
    public string Database { get; init; } = "HealthCasePlatform";
    public string CollectionName { get; init; } = "auditEntries";
}
