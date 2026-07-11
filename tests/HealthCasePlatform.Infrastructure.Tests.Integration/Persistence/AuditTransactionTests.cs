using HealthCasePlatform.Domain.Cases;
using HealthCasePlatform.Domain.Enums;
using HealthCasePlatform.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Shouldly;

namespace HealthCasePlatform.Infrastructure.Tests.Integration.Persistence;

public sealed class AuditTransactionTests : IClassFixture<DbFixture>
{
    private readonly DbFixture _fixture;

    public AuditTransactionTests(DbFixture fixture)
    {
        _fixture = fixture;
    }

    private AppDbContext CreateContext() => _fixture.CreateContext();

    private static RegulatoryCase CreateCase() =>
        RegulatoryCase.Create("Title", "Description", Guid.CreateVersion7(), CasePriority.High, "creator", "PL").Value;

    [Fact]
    public async Task SaveChanges_CaseAndAuditEntry_PersistedTogether()
    {
        var regulatoryCase = CreateCase();
        var audit = AuditEntry.Create(regulatoryCase.Id, AuditAction.CaseCreated, "creator", regulatoryCase.Title).Value;

        await using (var write = CreateContext())
        {
            await write.RegulatoryCases.AddAsync(regulatoryCase);
            await write.AuditEntries.AddAsync(audit);
            await write.SaveChangesAsync();
        }

        await using var read = CreateContext();
        var loadedCase = await read.RegulatoryCases.FindAsync(regulatoryCase.Id);
        var loadedAudit = await read.AuditEntries.FindAsync(audit.Id);

        loadedCase.ShouldNotBeNull();
        loadedAudit.ShouldNotBeNull();
        loadedAudit.CaseId.ShouldBe(regulatoryCase.Id);
        loadedAudit.Action.ShouldBe(AuditAction.CaseCreated);
    }

    [Fact]
    public async Task SaveChanges_CaseAndAuditEntry_RollBackTogether()
    {
        var regulatoryCase = CreateCase();
        var audit = AuditEntry.Create(regulatoryCase.Id, AuditAction.CaseCreated, "creator", regulatoryCase.Title).Value;

        await using (var write = CreateContext())
        {
            await write.Database.BeginTransactionAsync();
            await write.RegulatoryCases.AddAsync(regulatoryCase);
            await write.AuditEntries.AddAsync(audit);
            await write.SaveChangesAsync();
            await write.Database.RollbackTransactionAsync();
        }

        await using var read = CreateContext();
        var loadedCase = await read.RegulatoryCases.FindAsync(regulatoryCase.Id);
        var loadedAudit = await read.AuditEntries.FindAsync(audit.Id);

        loadedCase.ShouldBeNull();
        loadedAudit.ShouldBeNull();
    }

    [Fact]
    public async Task SaveChanges_WhenAuditInsertFails_CaseNotPersisted()
    {
        var regulatoryCase = CreateCase();
        var validAudit = AuditEntry.Create(regulatoryCase.Id, AuditAction.CaseCreated, "creator", regulatoryCase.Title).Value;
        var poisonAudit = AuditEntry.Create(regulatoryCase.Id, AuditAction.StatusChanged, "creator", "poison").Value;

        await using (var write = CreateContext())
        {
            await write.RegulatoryCases.AddAsync(regulatoryCase);
            await write.AuditEntries.AddAsync(validAudit);
            write.AuditEntries.Attach(poisonAudit);
            write.Entry(poisonAudit).State = EntityState.Added;
            write.Entry(poisonAudit).Property(x => x.Actor).CurrentValue = new string('x', 200);

            await Should.ThrowAsync<DbUpdateException>(write.SaveChangesAsync());
        }

        await using var read = CreateContext();
        var loadedCase = await read.RegulatoryCases.FindAsync(regulatoryCase.Id);
        var loadedValidAudit = await read.AuditEntries.FindAsync(validAudit.Id);

        loadedCase.ShouldBeNull();
        loadedValidAudit.ShouldBeNull();
    }
}
