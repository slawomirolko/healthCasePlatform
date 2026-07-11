using HealthCasePlatform.Domain.Cases;
using HealthCasePlatform.Domain.Cases.Events;
using HealthCasePlatform.Domain.Enums;
using Shouldly;

namespace HealthCasePlatform.Domain.Tests.Cases;

public class RegulatoryCaseTests
{
    private static RegulatoryCase CreateCase() =>
        RegulatoryCase.Create("Food safety incident #42", "Initial report", Guid.NewGuid(), CasePriority.High, "officer-1", "PL").Value;

    private static RegulatoryCase BringCaseTo(CaseStatus target)
    {
        var sut = CreateCase();
        if (target == CaseStatus.Draft)
        {
            return sut;
        }

        sut.Submit().IsError.ShouldBeFalse();
        if (target == CaseStatus.Submitted)
        {
            return sut;
        }

        sut.StartScientificReview().IsError.ShouldBeFalse();
        if (target == CaseStatus.UnderScientificReview)
        {
            return sut;
        }

        sut.StartLegalReview().IsError.ShouldBeFalse();
        if (target == CaseStatus.UnderLegalReview)
        {
            return sut;
        }

        sut.RequestDecision().IsError.ShouldBeFalse();
        if (target == CaseStatus.PendingDecision)
        {
            return sut;
        }

        if (target == CaseStatus.Approved)
        {
            sut.Approve().IsError.ShouldBeFalse();
            return sut;
        }

        if (target == CaseStatus.Rejected)
        {
            sut.Reject().IsError.ShouldBeFalse();
            return sut;
        }

        throw new ArgumentOutOfRangeException(nameof(target), $"Unsupported target status for test factory: {target}");
    }

    [Fact]
    public void Create_WithValidArguments_SetsPropertiesCorrectly()
    {
        var caseTypeId = Guid.NewGuid();

        var result = RegulatoryCase.Create("Title", "Desc", caseTypeId, CasePriority.Medium, "officer-1", "PL");

        result.IsError.ShouldBeFalse();
        result.Value.Id.ShouldNotBe(Guid.Empty);
        result.Value.Title.ShouldBe("Title");
        result.Value.Description.ShouldBe("Desc");
        result.Value.CaseTypeId.ShouldBe(caseTypeId);
        result.Value.Priority.ShouldBe(CasePriority.Medium);
        result.Value.Country.ShouldBe("PL");
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
        var result = RegulatoryCase.Create("", "desc", Guid.NewGuid(), CasePriority.Low, "officer-1", "PL");

        result.IsError.ShouldBeTrue();
    }

    [Fact]
    public void Create_WithWhitespaceTitle_ReturnsValidationError()
    {
        var result = RegulatoryCase.Create("   ", "desc", Guid.NewGuid(), CasePriority.Low, "officer-1", "PL");

        result.IsError.ShouldBeTrue();
        result.Errors.ShouldContain(e => e.Code == RegulatoryCaseErrors.TitleEmpty.Code);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithEmptyCountry_ReturnsCountryEmptyError(string country)
    {
        var result = RegulatoryCase.Create("Title", "desc", Guid.NewGuid(), CasePriority.Low, "officer-1", country);

        result.IsError.ShouldBeTrue();
        result.Errors.ShouldContain(e => e.Code == RegulatoryCaseErrors.CountryEmpty.Code);
    }

    [Theory]
    [InlineData("POL")]
    [InlineData("P")]
    [InlineData("12")]
    public void Create_WithNonTwoLetterCountry_ReturnsCountryInvalidError(string country)
    {
        var result = RegulatoryCase.Create("Title", "desc", Guid.NewGuid(), CasePriority.Low, "officer-1", country);

        result.IsError.ShouldBeTrue();
        result.Errors.ShouldContain(e => e.Code == RegulatoryCaseErrors.CountryInvalid.Code);
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
    public void Create_WhenFresh_HasNoDomainEvents()
    {
        var sut = CreateCase();

        sut.DomainEvents.ShouldBeEmpty();
    }

    [Fact]
    public void Submit_FromDraft_RaisesCaseSubmittedDomainEvent()
    {
        var sut = CreateCase();

        sut.Submit();

        var evt = sut.DomainEvents.OfType<CaseSubmitted>().Single();
        evt.CaseId.ShouldBe(sut.Id);
    }

    [Fact]
    public void Submit_WhenNotDraft_DoesNotRaiseCaseSubmittedEvent()
    {
        var sut = CreateCase();
        sut.Submit();

        sut.Submit();

        sut.DomainEvents.OfType<CaseSubmitted>().Count().ShouldBe(1);
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

    [Fact]
    public void StartScientificReview_FromSubmitted_ChangesStatusToUnderScientificReview()
    {
        var sut = CreateCase();
        sut.Submit();

        var result = sut.StartScientificReview();

        result.IsError.ShouldBeFalse();
        sut.Status.ShouldBe(CaseStatus.UnderScientificReview);
        sut.UpdatedAt.ShouldNotBeNull();
    }

    [Fact]
    public void StartScientificReview_WhenNotSubmitted_ReturnsConflictError()
    {
        var sut = CreateCase();

        var result = sut.StartScientificReview();

        result.IsError.ShouldBeTrue();
        result.Errors.ShouldContain(e => e.Code == RegulatoryCaseErrors.NotSubmitted.Code);
    }

    [Fact]
    public void Submit_AppendsHistoryEntryWithFromDraftToSubmitted()
    {
        var sut = CreateCase();

        sut.Submit();

        sut.History.Count.ShouldBe(1);
        sut.History[0].FromStatus.ShouldBe(CaseStatus.Draft);
        sut.History[0].ToStatus.ShouldBe(CaseStatus.Submitted);
        sut.History[0].CaseId.ShouldBe(sut.Id);
    }

    [Fact]
    public void StartReview_AppendsHistoryEntryWithFromSubmittedToUnderReview()
    {
        var sut = CreateCase();
        sut.Submit();

        sut.StartReview();

        sut.History.Count.ShouldBe(2);
        sut.History[^1].FromStatus.ShouldBe(CaseStatus.Submitted);
        sut.History[^1].ToStatus.ShouldBe(CaseStatus.UnderReview);
    }

    [Fact]
    public void StartScientificReview_AppendsHistoryEntryWithFromSubmittedToUnderScientificReview()
    {
        var sut = CreateCase();
        sut.Submit();

        sut.StartScientificReview();

        sut.History.Count.ShouldBe(2);
        sut.History[^1].FromStatus.ShouldBe(CaseStatus.Submitted);
        sut.History[^1].ToStatus.ShouldBe(CaseStatus.UnderScientificReview);
    }

    [Fact]
    public void Transitions_RecordHistoryInChronologicalOrder()
    {
        var sut = CreateCase();
        sut.Submit();
        sut.StartReview();

        sut.History.Count.ShouldBe(2);
        sut.History[0].TransitionedAt.ShouldBeLessThanOrEqualTo(sut.History[1].TransitionedAt);
        sut.History[0].Id.ShouldNotBe(sut.History[1].Id);
    }

    [Fact]
    public void ChangeStatus_AppendsHistoryEntryWithFromCurrentToNew()
    {
        var sut = CreateCase();
        sut.Submit();

        var result = sut.ChangeStatus(CaseStatus.PendingDecision);

        result.IsError.ShouldBeFalse();
        sut.History.Count.ShouldBe(2);
        sut.History[^1].FromStatus.ShouldBe(CaseStatus.Submitted);
        sut.History[^1].ToStatus.ShouldBe(CaseStatus.PendingDecision);
    }

    [Fact]
    public void Transition_RecordsSingleTimestampForHistoryAndUpdatedAt()
    {
        var sut = CreateCase();

        sut.Submit();

        sut.History[^1].TransitionedAt.ShouldBe(sut.UpdatedAt!.Value);
    }

    [Fact]
    public void StartLegalReview_FromUnderScientificReview_ChangesStatusToUnderLegalReview()
    {
        var sut = BringCaseTo(CaseStatus.UnderScientificReview);

        var result = sut.StartLegalReview();

        result.IsError.ShouldBeFalse();
        sut.Status.ShouldBe(CaseStatus.UnderLegalReview);
        sut.UpdatedAt.ShouldNotBeNull();
    }

    [Fact]
    public void RequestDecision_FromUnderLegalReview_ChangesStatusToPendingDecision()
    {
        var sut = BringCaseTo(CaseStatus.UnderLegalReview);

        var result = sut.RequestDecision();

        result.IsError.ShouldBeFalse();
        sut.Status.ShouldBe(CaseStatus.PendingDecision);
        sut.UpdatedAt.ShouldNotBeNull();
    }

    [Fact]
    public void Approve_FromPendingDecision_ChangesStatusToApproved()
    {
        var sut = BringCaseTo(CaseStatus.PendingDecision);

        var result = sut.Approve();

        result.IsError.ShouldBeFalse();
        sut.Status.ShouldBe(CaseStatus.Approved);
        sut.UpdatedAt.ShouldNotBeNull();
    }

    [Fact]
    public void Reject_FromPendingDecision_ChangesStatusToRejected()
    {
        var sut = BringCaseTo(CaseStatus.PendingDecision);

        var result = sut.Reject();

        result.IsError.ShouldBeFalse();
        sut.Status.ShouldBe(CaseStatus.Rejected);
        sut.UpdatedAt.ShouldNotBeNull();
    }

    [Fact]
    public void StartLegalReview_WhenNotUnderScientificReview_ReturnsConflictError()
    {
        var sut = BringCaseTo(CaseStatus.Submitted);

        var result = sut.StartLegalReview();

        result.IsError.ShouldBeTrue();
        result.Errors.ShouldContain(e => e.Code == RegulatoryCaseErrors.NotUnderScientificReview.Code);
    }

    [Fact]
    public void RequestDecision_WhenNotUnderLegalReview_ReturnsConflictError()
    {
        var sut = BringCaseTo(CaseStatus.UnderScientificReview);

        var result = sut.RequestDecision();

        result.IsError.ShouldBeTrue();
        result.Errors.ShouldContain(e => e.Code == RegulatoryCaseErrors.NotUnderLegalReview.Code);
    }

    [Fact]
    public void Approve_WhenNotPendingDecision_ReturnsConflictError()
    {
        var sut = BringCaseTo(CaseStatus.UnderLegalReview);

        var result = sut.Approve();

        result.IsError.ShouldBeTrue();
        result.Errors.ShouldContain(e => e.Code == RegulatoryCaseErrors.NotPendingDecision.Code);
    }

    [Fact]
    public void Reject_WhenNotPendingDecision_ReturnsConflictError()
    {
        var sut = BringCaseTo(CaseStatus.UnderLegalReview);

        var result = sut.Reject();

        result.IsError.ShouldBeTrue();
        result.Errors.ShouldContain(e => e.Code == RegulatoryCaseErrors.NotPendingDecision.Code);
    }

    [Fact]
    public void Approve_WhenAlreadyApproved_ReturnsInTerminalStateError()
    {
        var sut = BringCaseTo(CaseStatus.Approved);

        var result = sut.Approve();

        result.IsError.ShouldBeTrue();
        result.Errors.ShouldContain(e => e.Code == RegulatoryCaseErrors.InTerminalState.Code);
    }

    [Fact]
    public void Reject_WhenAlreadyRejected_ReturnsInTerminalStateError()
    {
        var sut = BringCaseTo(CaseStatus.Rejected);

        var result = sut.Reject();

        result.IsError.ShouldBeTrue();
        result.Errors.ShouldContain(e => e.Code == RegulatoryCaseErrors.InTerminalState.Code);
    }

    public static IEnumerable<object[]> TransitionHistoryCases =>
    [
        [CaseStatus.UnderScientificReview, nameof(RegulatoryCase.StartLegalReview), CaseStatus.UnderLegalReview],
        [CaseStatus.UnderLegalReview, nameof(RegulatoryCase.RequestDecision), CaseStatus.PendingDecision],
        [CaseStatus.PendingDecision, nameof(RegulatoryCase.Approve), CaseStatus.Approved],
        [CaseStatus.PendingDecision, nameof(RegulatoryCase.Reject), CaseStatus.Rejected]
    ];

    [Theory]
    [MemberData(nameof(TransitionHistoryCases))]
    public void Transitions_AppendHistoryEntryWithCorrectFromAndTo(
        CaseStatus preState, string transition, CaseStatus expectedTo)
    {
        var sut = BringCaseTo(preState);

        var result = transition switch
        {
            nameof(RegulatoryCase.StartLegalReview) => sut.StartLegalReview(),
            nameof(RegulatoryCase.RequestDecision) => sut.RequestDecision(),
            nameof(RegulatoryCase.Approve) => sut.Approve(),
            nameof(RegulatoryCase.Reject) => sut.Reject(),
            _ => throw new ArgumentOutOfRangeException(nameof(transition))
        };

        result.IsError.ShouldBeFalse();
        sut.History[^1].FromStatus.ShouldBe(preState);
        sut.History[^1].ToStatus.ShouldBe(expectedTo);
    }

    [Fact]
    public void FullScientificTrack_FromDraftToApproved_RecordsChronologicalHistory()
    {
        var sut = CreateCase();
        sut.Submit();
        sut.StartScientificReview();
        sut.StartLegalReview();
        sut.RequestDecision();
        sut.Approve();

        sut.Status.ShouldBe(CaseStatus.Approved);
        sut.History.Count.ShouldBe(5);
        for (var i = 1; i < sut.History.Count; i++)
        {
            sut.History[i].TransitionedAt.ShouldBeGreaterThanOrEqualTo(sut.History[i - 1].TransitionedAt);
        }
    }

    [Fact]
    public void AssignScientificReviewer_SetsAssignedScientificReviewerId()
    {
        var sut = CreateCase();

        var result = sut.AssignScientificReviewer("sci-1");

        result.IsError.ShouldBeFalse();
        sut.AssignedScientificReviewerId.ShouldBe("sci-1");
        sut.UpdatedAt.ShouldNotBeNull();
    }

    [Fact]
    public void AssignScientificReviewer_WhenReviewerIdEmpty_ReturnsReviewerIdEmptyError()
    {
        var sut = CreateCase();

        var result = sut.AssignScientificReviewer("  ");

        result.IsError.ShouldBeTrue();
        result.Errors.ShouldContain(e => e.Code == RegulatoryCaseErrors.ReviewerIdEmpty.Code);
        sut.AssignedScientificReviewerId.ShouldBeNull();
    }

    [Fact]
    public void AssignScientificReviewer_CanReassignExistingReviewer()
    {
        var sut = CreateCase();
        sut.AssignScientificReviewer("sci-1").IsError.ShouldBeFalse();

        var result = sut.AssignScientificReviewer("sci-2");

        result.IsError.ShouldBeFalse();
        sut.AssignedScientificReviewerId.ShouldBe("sci-2");
    }

    [Fact]
    public void AssignLegalReviewer_SetsAssignedLegalReviewerId()
    {
        var sut = CreateCase();

        var result = sut.AssignLegalReviewer("legal-1");

        result.IsError.ShouldBeFalse();
        sut.AssignedLegalReviewerId.ShouldBe("legal-1");
        sut.UpdatedAt.ShouldNotBeNull();
    }

    [Fact]
    public void AssignLegalReviewer_WhenReviewerIdEmpty_ReturnsReviewerIdEmptyError()
    {
        var sut = CreateCase();

        var result = sut.AssignLegalReviewer("");

        result.IsError.ShouldBeTrue();
        result.Errors.ShouldContain(e => e.Code == RegulatoryCaseErrors.ReviewerIdEmpty.Code);
        sut.AssignedLegalReviewerId.ShouldBeNull();
    }
}
