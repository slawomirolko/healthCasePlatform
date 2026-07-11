using HealthCasePlatform.Domain.Cases;
using HealthCasePlatform.Domain.Enums;
using Shouldly;

namespace HealthCasePlatform.Domain.Tests.Cases;

public class AuditEntryTests
{
    [Fact]
    public void Create_WhenValid_ReturnsEntryWithExpectedFields()
    {
        var caseId = Guid.NewGuid();

        var result = AuditEntry.Create(caseId, AuditAction.CaseCreated, "officer-1", "Food safety incident #42");

        result.IsError.ShouldBeFalse();
        var entry = result.Value;
        entry.Id.ShouldNotBe(Guid.Empty);
        entry.CaseId.ShouldBe(caseId);
        entry.Action.ShouldBe(AuditAction.CaseCreated);
        entry.Actor.ShouldBe("officer-1");
        entry.Detail.ShouldBe("Food safety incident #42");
        entry.OccurredAt.ShouldBeGreaterThan(DateTime.MinValue);
    }

    [Fact]
    public void Create_WhenCaseIdEmpty_ReturnsCaseIdEmptyError()
    {
        var result = AuditEntry.Create(Guid.Empty, AuditAction.CaseCreated, "officer-1");

        result.IsError.ShouldBeTrue();
        result.Errors.ShouldContain(AuditEntryErrors.CaseIdEmpty);
    }

    [Fact]
    public void Create_WhenActorEmpty_ReturnsActorEmptyError()
    {
        var result = AuditEntry.Create(Guid.NewGuid(), AuditAction.CaseCreated, "");

        result.IsError.ShouldBeTrue();
        result.Errors.ShouldContain(AuditEntryErrors.ActorEmpty);
    }
}
