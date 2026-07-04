using HealthCasePlatform.Domain.Cases;
using HealthCasePlatform.Domain.Enums;
using Shouldly;

namespace HealthCasePlatform.Domain.Tests.Cases;

public class RegulatoryCaseTests
{
    private static RegulatoryCase CreateCase() =>
        RegulatoryCase.Create("Food safety incident #42", "Initial report", Guid.NewGuid(), CasePriority.High, "officer-1").Value;

    [Fact]
    public void Create_WithValidArguments_SetsPropertiesCorrectly()
    {
        var caseTypeId = Guid.NewGuid();

        var result = RegulatoryCase.Create("Title", "Desc", caseTypeId, CasePriority.Medium, "officer-1");

        result.IsError.ShouldBeFalse();
        result.Value.Id.ShouldNotBe(Guid.Empty);
        result.Value.Title.ShouldBe("Title");
        result.Value.Description.ShouldBe("Desc");
        result.Value.CaseTypeId.ShouldBe(caseTypeId);
        result.Value.Priority.ShouldBe(CasePriority.Medium);
        result.Value.CreatedBy.ShouldBe("officer-1");
        result.Value.CreatedAt.ShouldBeGreaterThan(DateTime.MinValue);
    }

    [Fact]
    public void Create_InitializesAllCollectionsEmpty()
    {
        var sut = CreateCase();

        sut.Documents.ShouldBeEmpty();
        sut.Tasks.ShouldBeEmpty();
        sut.Comments.ShouldBeEmpty();
        sut.Decisions.ShouldBeEmpty();
    }

    [Fact]
    public void Create_SetsInitialStatusToDraft()
    {
        var sut = CreateCase();

        sut.Status.ShouldBe(CaseStatus.Draft);
    }

    [Fact]
    public void Create_WithEmptyTitle_ReturnsValidationError()
    {
        var result = RegulatoryCase.Create("", "desc", Guid.NewGuid(), CasePriority.Low, "officer-1");

        result.IsError.ShouldBeTrue();
    }

    [Fact]
    public void Submit_FromDraft_ChangesStatusToSubmitted()
    {
        var sut = CreateCase();

        var result = sut.Submit();

        result.IsError.ShouldBeFalse();
        sut.Status.ShouldBe(CaseStatus.Submitted);
        sut.UpdatedAt.ShouldNotBeNull();
    }

    [Fact]
    public void Submit_WhenNotDraft_ReturnsConflictError()
    {
        var sut = CreateCase();
        sut.Submit();

        var result = sut.Submit();

        result.IsError.ShouldBeTrue();
    }

    [Fact]
    public void StartReview_FromSubmitted_ChangesStatusToUnderReview()
    {
        var sut = CreateCase();
        sut.Submit();

        var result = sut.StartReview();

        result.IsError.ShouldBeFalse();
        sut.Status.ShouldBe(CaseStatus.UnderReview);
    }

    [Fact]
    public void StartReview_WhenNotSubmitted_ReturnsConflictError()
    {
        var sut = CreateCase();

        var result = sut.StartReview();

        result.IsError.ShouldBeTrue();
    }

    [Fact]
    public void ChangePriority_UpdatesPriorityAndUpdatedAt()
    {
        var sut = CreateCase();

        sut.ChangePriority(CasePriority.Critical);

        sut.Priority.ShouldBe(CasePriority.Critical);
        sut.UpdatedAt.ShouldNotBeNull();
    }

    [Fact]
    public void AddDocument_AddsToDocumentsCollection()
    {
        var sut = CreateCase();
        var document = CaseDocument.Create(sut.Id, "report.pdf", "application/pdf", "blobs/1", "officer-1").Value;

        var result = sut.AddDocument(document);

        result.IsError.ShouldBeFalse();
        sut.Documents.ShouldContain(document);
    }

    [Fact]
    public void AddTask_AddsToTasksCollection()
    {
        var sut = CreateCase();
        var task = CaseTask.Create(sut.Id, "Risk assessment", "reviewer-1").Value;

        var result = sut.AddTask(task);

        result.IsError.ShouldBeFalse();
        sut.Tasks.ShouldContain(task);
    }

    [Fact]
    public void AddComment_AddsToCommentsCollection()
    {
        var sut = CreateCase();
        var comment = Comment.Create(sut.Id, "Looks valid", "reviewer-1").Value;

        var result = sut.AddComment(comment);

        result.IsError.ShouldBeFalse();
        sut.Comments.ShouldContain(comment);
    }

    [Fact]
    public void RecordDecision_AddsToDecisionsCollection()
    {
        var sut = CreateCase();
        var decision = Decision.Create(sut.Id, "Approve with conditions", "leader-1").Value;

        var result = sut.RecordDecision(decision);

        result.IsError.ShouldBeFalse();
        sut.Decisions.ShouldContain(decision);
    }
}
