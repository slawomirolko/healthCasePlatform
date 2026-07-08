using HealthCasePlatform.Domain.Cases;
using HealthCasePlatform.Domain.Enums;
using HealthCasePlatform.Infrastructure.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Shouldly;

namespace HealthCasePlatform.Infrastructure.Tests.Persistence;

public sealed class AppDbContextTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly DbContextOptions<AppDbContext> _options;

    public AppDbContextTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();
        _options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(_connection)
            .Options;
        using var seed = CreateContext();
        seed.Database.EnsureCreated();
    }

    public void Dispose() => _connection.Dispose();

    private AppDbContext CreateContext() => new(_options);

    [Fact]
    public async Task SaveChanges_NewCase_CanBeRetrievedById()
    {
        var caseTypeId = Guid.CreateVersion7();
        var regulatoryCase = RegulatoryCase.Create("Title", "Description", caseTypeId, CasePriority.High, "creator", "PL").Value;

        await using (var write = CreateContext())
        {
            await write.RegulatoryCases.AddAsync(regulatoryCase);
            await write.SaveChangesAsync();
        }

        await using var read = CreateContext();
        var loaded = await read.RegulatoryCases.FindAsync(regulatoryCase.Id);

        loaded.ShouldNotBeNull();
        loaded.Title.ShouldBe("Title");
        loaded.Status.ShouldBe(CaseStatus.Draft);
        loaded.CaseTypeId.ShouldBe(caseTypeId);
        loaded.Country.ShouldBe("PL");
    }

    [Fact]
    public async Task SaveChanges_CaseWithDocuments_ReloadsDocumentsFromBackingField()
    {
        var regulatoryCase = RegulatoryCase.Create("Title", "Description", Guid.CreateVersion7(), CasePriority.Medium, "creator", "PL").Value;
        var first = CaseDocument.Create(regulatoryCase.Id, "a.pdf", "application/pdf", "blob/a", "uploader").Value;
        var second = CaseDocument.Create(regulatoryCase.Id, "b.pdf", "application/pdf", "blob/b", "uploader").Value;
        regulatoryCase.AddDocument(first);
        regulatoryCase.AddDocument(second);

        await using (var write = CreateContext())
        {
            await write.RegulatoryCases.AddAsync(regulatoryCase);
            await write.SaveChangesAsync();
        }

        await using var read = CreateContext();
        var loaded = await read.RegulatoryCases
            .Include(c => c.Documents)
            .SingleAsync(c => c.Id == regulatoryCase.Id);

        loaded.Documents.Count.ShouldBe(2);
        loaded.Documents.ShouldContain(d => d.FileName == "a.pdf");
        loaded.Documents.ShouldContain(d => d.FileName == "b.pdf");
    }

    [Fact]
    public async Task SaveChanges_CaseWithTasksAndCommentsAndDecisions_ReloadsAllChildren()
    {
        var regulatoryCase = RegulatoryCase.Create("Title", "Description", Guid.CreateVersion7(), CasePriority.Low, "creator", "PL").Value;
        regulatoryCase.AddTask(CaseTask.Create(regulatoryCase.Id, "Review", "assignee").Value);
        regulatoryCase.AddComment(Comment.Create(regulatoryCase.Id, "Looks good", "author").Value);
        regulatoryCase.RecordDecision(Decision.Create(regulatoryCase.Id, "Approved", "decider").Value);

        await using (var write = CreateContext())
        {
            await write.RegulatoryCases.AddAsync(regulatoryCase);
            await write.SaveChangesAsync();
        }

        await using var read = CreateContext();
        var loaded = await read.RegulatoryCases
            .Include(c => c.Tasks)
            .Include(c => c.Comments)
            .Include(c => c.Decisions)
            .SingleAsync(c => c.Id == regulatoryCase.Id);

        loaded.Tasks.Count.ShouldBe(1);
        loaded.Comments.Count.ShouldBe(1);
        loaded.Decisions.Count.ShouldBe(1);
        loaded.Tasks.Single().Title.ShouldBe("Review");
        loaded.Comments.Single().Content.ShouldBe("Looks good");
        loaded.Decisions.Single().DecisionText.ShouldBe("Approved");
    }

    [Fact]
    public async Task SaveChanges_CaseType_PersistsAndCanBeLookedUpById()
    {
        var caseType = CaseType.Create("Pharmaceutical", "Pharma cases").Value;

        await using (var write = CreateContext())
        {
            await write.CaseTypes.AddAsync(caseType);
            await write.SaveChangesAsync();
        }

        await using var read = CreateContext();
        var loaded = await read.CaseTypes.FindAsync(caseType.Id);

        loaded.ShouldNotBeNull();
        loaded.Name.ShouldBe("Pharmaceutical");
        loaded.Description.ShouldBe("Pharma cases");
    }

    [Fact]
    public async Task SaveChanges_CaseStatusAndPriority_PersistAsExpectedEnumValues()
    {
        var regulatoryCase = RegulatoryCase.Create("Title", "Description", Guid.CreateVersion7(), CasePriority.Critical, "creator", "PL").Value;
        regulatoryCase.Submit().IsError.ShouldBeFalse();

        await using (var write = CreateContext())
        {
            await write.RegulatoryCases.AddAsync(regulatoryCase);
            await write.SaveChangesAsync();
        }

        await using var read = CreateContext();
        var loaded = await read.RegulatoryCases.FindAsync(regulatoryCase.Id);

        loaded.ShouldNotBeNull();
        loaded.Status.ShouldBe(CaseStatus.Submitted);
        loaded.Priority.ShouldBe(CasePriority.Critical);
        loaded.Country.ShouldBe("PL");
    }

    [Fact]
    public async Task SaveChanges_CaseWithHistory_ReloadsHistoryFromBackingField()
    {
        var regulatoryCase = RegulatoryCase.Create("Title", "Description", Guid.CreateVersion7(), CasePriority.Medium, "creator", "PL").Value;
        regulatoryCase.Submit();
        regulatoryCase.StartReview();

        await using (var write = CreateContext())
        {
            await write.RegulatoryCases.AddAsync(regulatoryCase);
            await write.SaveChangesAsync();
        }

        await using var read = CreateContext();
        var loaded = await read.RegulatoryCases
            .Include(c => c.History)
            .SingleAsync(c => c.Id == regulatoryCase.Id);

        loaded.History.Count.ShouldBe(2);
        loaded.History.ShouldContain(h => h.FromStatus == CaseStatus.Draft && h.ToStatus == CaseStatus.Submitted);
        loaded.History.ShouldContain(h => h.FromStatus == CaseStatus.Submitted && h.ToStatus == CaseStatus.UnderReview);
    }
}
