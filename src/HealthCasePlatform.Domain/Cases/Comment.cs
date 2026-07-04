using ErrorOr;
using HealthCasePlatform.Domain.Common;

namespace HealthCasePlatform.Domain.Cases;

public sealed class Comment : Entity
{
    public Guid CaseId { get; private set; }
    public string Content { get; private set; }
    public string Author { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? EditedAt { get; private set; }

    private Comment() { }

    public static ErrorOr<Comment> Create(Guid caseId, string content, string author)
    {
        if (caseId == Guid.Empty)
        {
            return CommentErrors.CaseIdEmpty;
        }

        if (string.IsNullOrWhiteSpace(content))
        {
            return CommentErrors.ContentEmpty;
        }

        if (string.IsNullOrWhiteSpace(author))
        {
            return CommentErrors.AuthorEmpty;
        }

        return new Comment
        {
            Id = Guid.CreateVersion7(),
            CaseId = caseId,
            Content = content,
            Author = author,
            CreatedAt = DateTime.UtcNow,
            EditedAt = null
        };
    }

    public ErrorOr<Success> Edit(string newContent)
    {
        if (string.IsNullOrWhiteSpace(newContent))
        {
            return CommentErrors.NewContentEmpty;
        }

        Content = newContent;
        EditedAt = DateTime.UtcNow;
        return Result.Success;
    }
}
