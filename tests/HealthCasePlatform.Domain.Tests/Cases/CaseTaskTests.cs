using HealthCasePlatform.Domain.Cases;
using Shouldly;

namespace HealthCasePlatform.Domain.Tests.Cases;

public class CaseTaskTests
{
    [Fact]
    public void Create_SetsPropertiesCorrectly()
    {
        var caseId = Guid.NewGuid();
        var due = new DateTime(2026, 12, 31, 0, 0, 0, DateTimeKind.Utc);

        var result = CaseTask.Create(caseId, "Risk assessment", "reviewer-1", due);

        result.IsError.ShouldBeFalse();
        result.Value.Id.ShouldNotBe(Guid.Empty);
        result.Value.CaseId.ShouldBe(caseId);
        result.Value.Title.ShouldBe("Risk assessment");
        result.Value.AssignedTo.ShouldBe("reviewer-1");
        result.Value.DueDate.ShouldBe(due);
        result.Value.IsCompleted.ShouldBeFalse();
        result.Value.CompletedAt.ShouldBeNull();
    }

    [Fact]
    public void Complete_SetsIsCompletedAndCompletedAt()
    {
        var sut = CaseTask.Create(Guid.NewGuid(), "Title", "reviewer-1").Value;

        var result = sut.Complete();

        result.IsError.ShouldBeFalse();
        sut.IsCompleted.ShouldBeTrue();
        sut.CompletedAt.ShouldNotBeNull();
    }

    [Fact]
    public void Complete_WhenAlreadyCompleted_ReturnsConflictError()
    {
        var sut = CaseTask.Create(Guid.NewGuid(), "Title", "reviewer-1").Value;
        sut.Complete();

        var result = sut.Complete();

        result.IsError.ShouldBeTrue();
    }

    [Fact]
    public void Reassign_UpdatesAssignedTo()
    {
        var sut = CaseTask.Create(Guid.NewGuid(), "Title", "reviewer-1").Value;

        var result = sut.Reassign("reviewer-2");

        result.IsError.ShouldBeFalse();
        sut.AssignedTo.ShouldBe("reviewer-2");
    }

    [Fact]
    public void Reassign_WithEmptyAssignee_ReturnsValidationError()
    {
        var sut = CaseTask.Create(Guid.NewGuid(), "Title", "reviewer-1").Value;

        var result = sut.Reassign("");

        result.IsError.ShouldBeTrue();
    }

    [Fact]
    public void IsOverdue_WhenDueDatePastAndNotCompleted_ReturnsTrue()
    {
        var pastDue = DateTime.UtcNow.AddHours(-1);
        var sut = CaseTask.Create(Guid.NewGuid(), "Title", "reviewer-1", pastDue).Value;

        sut.IsOverdue().ShouldBeTrue();
    }

    [Fact]
    public void IsOverdue_WhenDueDateFuture_ReturnsFalse()
    {
        var futureDue = DateTime.UtcNow.AddHours(1);
        var sut = CaseTask.Create(Guid.NewGuid(), "Title", "reviewer-1", futureDue).Value;

        sut.IsOverdue().ShouldBeFalse();
    }

    [Fact]
    public void IsOverdue_WhenCompleted_ReturnsFalse()
    {
        var pastDue = DateTime.UtcNow.AddHours(-1);
        var sut = CaseTask.Create(Guid.NewGuid(), "Title", "reviewer-1", pastDue).Value;
        sut.Complete();

        sut.IsOverdue().ShouldBeFalse();
    }
}
