using HealthCasePlatform.Domain.Cases;
using Shouldly;

namespace HealthCasePlatform.Domain.Tests.Cases;

public class CommentTests
{
    [Fact]
    public void Create_SetsContentAuthorAndCreatedAt()
    {
        var caseId = Guid.NewGuid();

        var result = Comment.Create(caseId, "Looks valid", "reviewer-1");

        result.IsError.ShouldBeFalse();
        result.Value.Id.ShouldNotBe(Guid.Empty);
        result.Value.CaseId.ShouldBe(caseId);
        result.Value.Content.ShouldBe("Looks valid");
        result.Value.Author.ShouldBe("reviewer-1");
        result.Value.CreatedAt.ShouldBeGreaterThan(DateTime.MinValue);
        result.Value.EditedAt.ShouldBeNull();
    }

    [Fact]
    public void Create_WithEmptyContent_ReturnsValidationError()
    {
        var result = Comment.Create(Guid.NewGuid(), "", "reviewer-1");

        result.IsError.ShouldBeTrue();
    }

    [Fact]
    public void Edit_UpdatesContentAndSetsEditedAt()
    {
        var sut = Comment.Create(Guid.NewGuid(), "Original", "reviewer-1").Value;

        var result = sut.Edit("Updated content");

        result.IsError.ShouldBeFalse();
        sut.Content.ShouldBe("Updated content");
        sut.EditedAt.ShouldNotBeNull();
    }

    [Fact]
    public void Edit_WithEmptyContent_ReturnsValidationError()
    {
        var sut = Comment.Create(Guid.NewGuid(), "Original", "reviewer-1").Value;

        var result = sut.Edit("");

        result.IsError.ShouldBeTrue();
    }
}
