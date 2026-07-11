using HealthCasePlatform.Domain.Cases;
using HealthCasePlatform.Infrastructure.Persistence.Mongo;
using HealthCasePlatform.Infrastructure.Tests.Integration.Persistence.Mongo;
using MongoDB.Driver;

namespace HealthCasePlatform.Infrastructure.Tests.Integration.Persistence.Audit;

public sealed class MongoAuditLogWriterContractTests : AuditLogWriterContractTests, IClassFixture<MongoDbFixture>
{
    private readonly MongoDbFixture _fixture;

    public MongoAuditLogWriterContractTests(MongoDbFixture fixture)
    {
        _fixture = fixture;
    }

    protected override IAuditLogWriter CreateWriter()
    {
        AuditEntryClassMap.Register();
        var db = new MongoClient(_fixture.ConnectionString).GetDatabase("test");
        return new MongoAuditLogWriter(db, "auditEntries");
    }

    protected override Task<Guid> SeedCaseAsync(CancellationToken ct) => Task.FromResult(Guid.NewGuid());
}
