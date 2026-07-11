using HealthCasePlatform.Domain.Cases.Messaging;
using Shouldly;

namespace HealthCasePlatform.Domain.Tests.Cases.Messaging;

public class OutboxMessageTests
{
    [Fact]
    public void Create_WithValidInput_ReturnsEntryWithExpectedFields()
    {
        var entry = OutboxMessage.Create("CaseSubmitted", """{"CaseId":"00000000-0000-0000-0000-000000000001"}""");

        entry.Id.ShouldNotBe(Guid.Empty);
        entry.Type.ShouldBe("CaseSubmitted");
        entry.Payload.ShouldContain("CaseId");
        entry.OccurredAtUtc.ShouldBeGreaterThan(DateTime.MinValue);
        entry.ProcessedAtUtc.ShouldBeNull();
        entry.Attempts.ShouldBe(0);
        entry.LastError.ShouldBeNull();
    }
}
