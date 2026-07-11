using HealthCasePlatform.Domain.Cases;
using HealthCasePlatform.Domain.Enums;
using Shouldly;

namespace HealthCasePlatform.Infrastructure.Tests.Integration.Persistence.Audit;

public abstract class AuditLogWriterContractTests
{
    protected abstract IAuditLogWriter CreateWriter();

    protected abstract Task<Guid> SeedCaseAsync(CancellationToken ct);

    protected virtual Task CommitAsync(CancellationToken ct) => Task.CompletedTask;

    [Fact]
    public async Task WriteAsync_ThenGetByCaseId_PersistsAllFields()
    {
        var ct = CancellationToken.None;
        var writer = CreateWriter();
        var caseId = await SeedCaseAsync(ct);
        var entry = AuditEntry.Create(caseId, AuditAction.CaseCreated, "officer-1", "Detail here").Value;

        await writer.WriteAsync(entry, ct);
        await CommitAsync(ct);

        var result = await writer.GetByCaseIdAsync(caseId, ct);
        var loaded = result.Single();

        loaded.Id.ShouldBe(entry.Id);
        loaded.CaseId.ShouldBe(entry.CaseId);
        loaded.Action.ShouldBe(entry.Action);
        loaded.Actor.ShouldBe(entry.Actor);
        loaded.Detail.ShouldBe(entry.Detail);
        loaded.OccurredAt.ShouldBe(entry.OccurredAt, TimeSpan.FromMilliseconds(1));
    }

    [Fact]
    public async Task GetByCaseId_ReturnsEntriesOrderedByOccurredAtThenId()
    {
        var ct = CancellationToken.None;
        var writer = CreateWriter();
        var caseId = await SeedCaseAsync(ct);

        var entry1 = AuditEntry.Create(caseId, AuditAction.CaseCreated, "actor1", "d1").Value;
        await writer.WriteAsync(entry1, ct);
        await CommitAsync(ct);
        await Task.Delay(20, ct);

        var entry2 = AuditEntry.Create(caseId, AuditAction.StatusChanged, "actor2", "d2").Value;
        await writer.WriteAsync(entry2, ct);
        await CommitAsync(ct);
        await Task.Delay(20, ct);

        var entry3 = AuditEntry.Create(caseId, AuditAction.DecisionMade, "actor3", "d3").Value;
        await writer.WriteAsync(entry3, ct);
        await CommitAsync(ct);

        var result = await writer.GetByCaseIdAsync(caseId, ct);

        result.Count.ShouldBe(3);
        result[0].Id.ShouldBe(entry1.Id);
        result[1].Id.ShouldBe(entry2.Id);
        result[2].Id.ShouldBe(entry3.Id);
    }

    [Fact]
    public async Task GetByCaseId_WhenNoEntries_ReturnsEmpty()
    {
        var ct = CancellationToken.None;
        var writer = CreateWriter();

        var result = await writer.GetByCaseIdAsync(Guid.NewGuid(), ct);

        result.ShouldBeEmpty();
    }

    [Fact]
    public async Task GetByCaseId_FiltersByCaseId()
    {
        var ct = CancellationToken.None;
        var writer = CreateWriter();
        var caseA = await SeedCaseAsync(ct);
        var caseB = await SeedCaseAsync(ct);

        var entryA = AuditEntry.Create(caseA, AuditAction.CaseCreated, "actor", "A").Value;
        var entryB = AuditEntry.Create(caseB, AuditAction.CaseCreated, "actor", "B").Value;
        await writer.WriteAsync(entryA, ct);
        await writer.WriteAsync(entryB, ct);
        await CommitAsync(ct);

        var result = await writer.GetByCaseIdAsync(caseA, ct);

        result.Count.ShouldBe(1);
        result.Single().Id.ShouldBe(entryA.Id);
    }

    [Theory]
    [InlineData(AuditAction.CaseCreated)]
    [InlineData(AuditAction.StatusChanged)]
    [InlineData(AuditAction.DecisionMade)]
    public async Task WriteAsync_PreservesAllAuditActionEnumValues(AuditAction action)
    {
        var ct = CancellationToken.None;
        var writer = CreateWriter();
        var caseId = await SeedCaseAsync(ct);
        var entry = AuditEntry.Create(caseId, action, "actor", "detail").Value;

        await writer.WriteAsync(entry, ct);
        await CommitAsync(ct);

        var result = await writer.GetByCaseIdAsync(caseId, ct);
        result.Single().Action.ShouldBe(action);
    }

    [Fact]
    public async Task WriteAsync_PreservesNullDetail()
    {
        var ct = CancellationToken.None;
        var writer = CreateWriter();
        var caseId = await SeedCaseAsync(ct);
        var entry = AuditEntry.Create(caseId, AuditAction.CaseCreated, "actor").Value;

        await writer.WriteAsync(entry, ct);
        await CommitAsync(ct);

        var result = await writer.GetByCaseIdAsync(caseId, ct);
        result.Single().Detail.ShouldBeNull();
    }

    [Fact]
    public async Task WriteAsync_PreservesUnicodeDetail()
    {
        var ct = CancellationToken.None;
        var writer = CreateWriter();
        var caseId = await SeedCaseAsync(ct);
        var entry = AuditEntry.Create(caseId, AuditAction.StatusChanged, "actor", "PendingDecision → Approved").Value;

        await writer.WriteAsync(entry, ct);
        await CommitAsync(ct);

        var result = await writer.GetByCaseIdAsync(caseId, ct);
        result.Single().Detail.ShouldBe("PendingDecision → Approved");
    }
}
