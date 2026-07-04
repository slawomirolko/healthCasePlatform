using HealthCasePlatform.Domain.Cases;
using Shouldly;

namespace HealthCasePlatform.Domain.Tests.Cases;

public class DecisionTests
{
    [Fact]
    public void Create_SetsPropertiesAndDecidedAt()
    {
        var caseId = Guid.NewGuid();

        var result = Decision.Create(caseId, "Approve with conditions", "leader-1");

        result.IsError.ShouldBeFalse();
        result.Value.Id.ShouldNotBe(Guid.Empty);
        result.Value.CaseId.ShouldBe(caseId);
        result.Value.DecisionText.ShouldBe("Approve with conditions");
        result.Value.DecidedBy.ShouldBe("leader-1");
        result.Value.DecidedAt.ShouldBeGreaterThan(DateTime.MinValue);
        result.Value.IsFinal.ShouldBeFalse();
    }

    [Fact]
    public void MarkFinal_SetsIsFinalTrue()
    {
        var sut = Decision.Create(Guid.NewGuid(), "Approve", "leader-1").Value;

        var result = sut.MarkFinal();

        result.IsError.ShouldBeFalse();
        sut.IsFinal.ShouldBeTrue();
    }

    [Fact]
    public void MarkFinal_WhenAlreadyFinal_ReturnsConflictError()
    {
        var sut = Decision.Create(Guid.NewGuid(), "Approve", "leader-1").Value;
        sut.MarkFinal();

        var result = sut.MarkFinal();

        result.IsError.ShouldBeTrue();
    }
}
