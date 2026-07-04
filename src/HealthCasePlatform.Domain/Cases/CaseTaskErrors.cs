using ErrorOr;

namespace HealthCasePlatform.Domain.Cases;

public static class CaseTaskErrors
{
    public static readonly Error CaseIdEmpty = Error.Validation("CaseTask.CaseIdEmpty", "Case ID cannot be empty.");
    public static readonly Error TitleEmpty = Error.Validation("CaseTask.TitleEmpty", "Task title cannot be empty.");
    public static readonly Error AssigneeEmpty = Error.Validation("CaseTask.AssigneeEmpty", "Assignee cannot be empty.");
    public static readonly Error NewAssigneeEmpty = Error.Validation("CaseTask.NewAssigneeEmpty", "New assignee cannot be empty.");
    public static readonly Error AlreadyCompleted = Error.Conflict("CaseTask.AlreadyCompleted", "Task is already completed.");
}
