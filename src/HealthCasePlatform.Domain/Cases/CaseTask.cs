using ErrorOr;
using HealthCasePlatform.Domain.Common;

namespace HealthCasePlatform.Domain.Cases;

public sealed class CaseTask : Entity
{
    public Guid CaseId { get; private set; }
    public string Title { get; private set; }
    public string? Description { get; private set; }
    public string AssignedTo { get; private set; }
    public DateTime? DueDate { get; private set; }
    public bool IsCompleted { get; private set; }
    public DateTime? CompletedAt { get; private set; }

    private CaseTask() { }

    public static ErrorOr<CaseTask> Create(Guid caseId, string title, string assignedTo, DateTime? dueDate = null)
    {
        if (caseId == Guid.Empty)
        {
            return CaseTaskErrors.CaseIdEmpty;
        }

        if (string.IsNullOrWhiteSpace(title))
        {
            return CaseTaskErrors.TitleEmpty;
        }

        if (string.IsNullOrWhiteSpace(assignedTo))
        {
            return CaseTaskErrors.AssigneeEmpty;
        }

        return new CaseTask
        {
            Id = Guid.CreateVersion7(),
            CaseId = caseId,
            Title = title,
            AssignedTo = assignedTo,
            DueDate = dueDate,
            IsCompleted = false,
            CompletedAt = null
        };
    }

    public ErrorOr<Success> Complete()
    {
        if (IsCompleted)
        {
            return CaseTaskErrors.AlreadyCompleted;
        }

        IsCompleted = true;
        CompletedAt = DateTime.UtcNow;
        return Result.Success;
    }

    public ErrorOr<Success> Reassign(string newAssignee)
    {
        if (string.IsNullOrWhiteSpace(newAssignee))
        {
            return CaseTaskErrors.NewAssigneeEmpty;
        }

        AssignedTo = newAssignee;
        return Result.Success;
    }

    public bool IsOverdue() =>
        !IsCompleted && DueDate is { } due && due < DateTime.UtcNow;
}
