using HealthCasePlatform.Domain.Cases;
using HealthCasePlatform.Domain.Enums;
using HealthCasePlatform.Infrastructure.Persistence;
using HealthCasePlatform.Infrastructure.Persistence.AuditStores;

namespace HealthCasePlatform.Infrastructure.Tests.Integration.Persistence.Audit;

public sealed class SqlAuditLogWriterContractTests : AuditLogWriterContractTests, IClassFixture<DbFixture>
{
    private readonly DbFixture _fixture;
    private AppDbContext? _ctx;

    public SqlAuditLogWriterContractTests(DbFixture fixture)
    {
        _fixture = fixture;
    }

    protected override IAuditLogWriter CreateWriter()
    {
        _ctx = _fixture.CreateContext();
        return new SqlAuditLogWriter(_ctx);
    }

    protected override async Task<Guid> SeedCaseAsync(CancellationToken ct)
    {
        var regulatoryCase = RegulatoryCase.Create("Title", "Description", Guid.CreateVersion7(), CasePriority.High, "creator", "PL").Value;
        await _ctx!.RegulatoryCases.AddAsync(regulatoryCase, ct);
        await _ctx.SaveChangesAsync(ct);
        return regulatoryCase.Id;
    }

    protected override Task CommitAsync(CancellationToken ct) => _ctx!.SaveChangesAsync(ct);
}
