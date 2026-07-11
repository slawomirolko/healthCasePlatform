using ErrorOr;
using HealthCasePlatform.Domain.Common;
using HealthCasePlatform.Domain.Enums;

namespace HealthCasePlatform.Domain.Cases;

public sealed class RegulatoryCase : Entity
{
    private static readonly HashSet<CaseStatus> TerminalStatuses =
    [
        CaseStatus.Approved,
        CaseStatus.Rejected,
        CaseStatus.Archived
    ];

    private readonly List<CaseDocument> _documents = [];
    private readonly List<CaseTask> _tasks = [];
    private readonly List<Comment> _comments = [];
    private readonly List<Decision> _decisions = [];
    private readonly List<CaseStatusHistory> _history = [];

    public string Title { get; private set; }
    public string Description { get; private set; }
    public Guid CaseTypeId { get; private set; }
    public CaseStatus Status { get; private set; }
    public CasePriority Priority { get; private set; }
    public string Country { get; private set; }
    public string CreatedBy { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }
    public string? AssignedScientificReviewerId { get; private set; }
    public string? AssignedLegalReviewerId { get; private set; }

    public IReadOnlyList<CaseDocument> Documents => _documents;
    public IReadOnlyList<CaseTask> Tasks => _tasks;
    public IReadOnlyList<Comment> Comments => _comments;
    public IReadOnlyList<Decision> Decisions => _decisions;
    public IReadOnlyList<CaseStatusHistory> History => _history;

    private RegulatoryCase() { }

    public static ErrorOr<RegulatoryCase> Create(string title, string description, Guid caseTypeId, CasePriority priority, string createdBy, string country)
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            return RegulatoryCaseErrors.TitleEmpty;
        }

        if (caseTypeId == Guid.Empty)
        {
            return RegulatoryCaseErrors.CaseTypeIdEmpty;
        }

        if (string.IsNullOrWhiteSpace(createdBy))
        {
            return RegulatoryCaseErrors.CreatedByEmpty;
        }

        if (string.IsNullOrWhiteSpace(country))
        {
            return RegulatoryCaseErrors.CountryEmpty;
        }

        var normalizedCountry = country.Trim().ToUpperInvariant();
        if (normalizedCountry.Length != 2 || !normalizedCountry.All(char.IsLetter))
        {
            return RegulatoryCaseErrors.CountryInvalid;
        }

        return new RegulatoryCase
        {
            Id = Guid.CreateVersion7(),
            Title = title,
            Description = description ?? string.Empty,
            CaseTypeId = caseTypeId,
            Priority = priority,
            Country = normalizedCountry,
            CreatedBy = createdBy,
            Status = CaseStatus.Draft,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = null
        };
    }

    public ErrorOr<Success> Submit()
        => Transition(CaseStatus.Draft, CaseStatus.Submitted, RegulatoryCaseErrors.NotDraft);

    public ErrorOr<Success> StartReview()
        => Transition(CaseStatus.Submitted, CaseStatus.UnderReview, RegulatoryCaseErrors.NotSubmitted);

    public ErrorOr<Success> StartScientificReview()
        => Transition(CaseStatus.Submitted, CaseStatus.UnderScientificReview, RegulatoryCaseErrors.NotSubmitted);

    public ErrorOr<Success> StartLegalReview()
        => Transition(CaseStatus.UnderScientificReview, CaseStatus.UnderLegalReview, RegulatoryCaseErrors.NotUnderScientificReview);

    public ErrorOr<Success> RequestDecision()
        => Transition(CaseStatus.UnderLegalReview, CaseStatus.PendingDecision, RegulatoryCaseErrors.NotUnderLegalReview);

    public ErrorOr<Success> Approve()
        => Transition(CaseStatus.PendingDecision, CaseStatus.Approved, RegulatoryCaseErrors.NotPendingDecision);

    public ErrorOr<Success> Reject()
        => Transition(CaseStatus.PendingDecision, CaseStatus.Rejected, RegulatoryCaseErrors.NotPendingDecision);

    public void ChangePriority(CasePriority newPriority)
    {
        Priority = newPriority;
        UpdatedAt = DateTime.UtcNow;
    }

    public ErrorOr<Success> AssignScientificReviewer(string reviewerId)
    {
        if (string.IsNullOrWhiteSpace(reviewerId))
        {
            return RegulatoryCaseErrors.ReviewerIdEmpty;
        }

        AssignedScientificReviewerId = reviewerId;
        UpdatedAt = DateTime.UtcNow;
        return Result.Success;
    }

    public ErrorOr<Success> AssignLegalReviewer(string reviewerId)
    {
        if (string.IsNullOrWhiteSpace(reviewerId))
        {
            return RegulatoryCaseErrors.ReviewerIdEmpty;
        }

        AssignedLegalReviewerId = reviewerId;
        UpdatedAt = DateTime.UtcNow;
        return Result.Success;
    }

    public ErrorOr<Success> ChangeStatus(CaseStatus newStatus)
    {
        if (TerminalStatuses.Contains(Status))
        {
            return RegulatoryCaseErrors.InTerminalState;
        }

        var now = DateTime.UtcNow;
        RecordTransition(newStatus, now);
        Status = newStatus;
        UpdatedAt = now;
        return Result.Success;
    }

    public ErrorOr<Success> AddDocument(CaseDocument document)
    {
        if (document is null)
        {
            return RegulatoryCaseErrors.DocumentNull;
        }

        _documents.Add(document);
        return Result.Success;
    }

    public ErrorOr<Success> AddTask(CaseTask task)
    {
        if (task is null)
        {
            return RegulatoryCaseErrors.TaskNull;
        }

        _tasks.Add(task);
        return Result.Success;
    }

    public ErrorOr<Success> AddComment(Comment comment)
    {
        if (comment is null)
        {
            return RegulatoryCaseErrors.CommentNull;
        }

        _comments.Add(comment);
        return Result.Success;
    }

    public ErrorOr<Success> RecordDecision(Decision decision)
    {
        if (decision is null)
        {
            return RegulatoryCaseErrors.DecisionNull;
        }

        _decisions.Add(decision);
        return Result.Success;
    }

    private void RecordTransition(CaseStatus toStatus, DateTime at)
    {
        _history.Add(CaseStatusHistory.Create(Id, Status, toStatus, at));
    }

    private ErrorOr<Success> Transition(CaseStatus expectedCurrent, CaseStatus target, Error onMismatch)
    {
        if (TerminalStatuses.Contains(Status))
        {
            return RegulatoryCaseErrors.InTerminalState;
        }

        if (Status != expectedCurrent)
        {
            return onMismatch;
        }

        var now = DateTime.UtcNow;
        RecordTransition(target, now);
        Status = target;
        UpdatedAt = now;
        return Result.Success;
    }
}
