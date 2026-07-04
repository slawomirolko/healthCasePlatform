using ErrorOr;

namespace HealthCasePlatform.Domain.Cases;

public static class CommentErrors
{
    public static readonly Error CaseIdEmpty = Error.Validation("Comment.CaseIdEmpty", "Case ID cannot be empty.");
    public static readonly Error ContentEmpty = Error.Validation("Comment.ContentEmpty", "Comment content cannot be empty.");
    public static readonly Error AuthorEmpty = Error.Validation("Comment.AuthorEmpty", "Author cannot be empty.");
    public static readonly Error NewContentEmpty = Error.Validation("Comment.NewContentEmpty", "Comment content cannot be empty.");
}
