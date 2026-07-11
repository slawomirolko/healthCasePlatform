using HealthCasePlatform.Domain.Cases;
using HealthCasePlatform.Domain.Enums;
using Shouldly;

namespace HealthCasePlatform.Domain.Tests.Cases;

public class NotificationTests
{
    [Fact]
    public void Create_WhenValid_ReturnsNotificationWithExpectedFields()
    {
        var caseId = Guid.NewGuid();

        var result = Notification.Create(caseId, NotificationType.CaseSubmitted);

        result.IsError.ShouldBeFalse();
        result.Value.Id.ShouldNotBe(Guid.Empty);
        result.Value.CaseId.ShouldBe(caseId);
        result.Value.Type.ShouldBe(NotificationType.CaseSubmitted);
        result.Value.CreatedAt.ShouldBeGreaterThan(DateTime.MinValue);
    }

    [Fact]
    public void Create_WhenCaseIdEmpty_ReturnsCaseIdEmptyError()
    {
        var result = Notification.Create(Guid.Empty, NotificationType.CaseSubmitted);

        result.IsError.ShouldBeTrue();
        result.Errors.ShouldContain(NotificationErrors.CaseIdEmpty);
    }

    [Fact]
    public void Create_WhenTypeInvalid_ReturnsTypeInvalidError()
    {
        var result = Notification.Create(Guid.NewGuid(), (NotificationType)999);

        result.IsError.ShouldBeTrue();
        result.Errors.ShouldContain(NotificationErrors.TypeInvalid);
    }
}
