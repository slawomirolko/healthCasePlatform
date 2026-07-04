namespace HealthCasePlatform.Infrastructure.Persistence;

public sealed record DatabaseSettings
{
    public const string SectionName = "Database";
    public string ConnectionString { get; init; } = string.Empty;
}
