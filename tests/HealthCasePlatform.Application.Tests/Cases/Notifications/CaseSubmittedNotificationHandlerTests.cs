using HealthCasePlatform.Application.Cases.Notifications;
using HealthCasePlatform.Domain.Cases;
using HealthCasePlatform.Domain.Enums;
using NSubstitute;
using Shouldly;

namespace HealthCasePlatform.Application.Tests.Cases.Notifications;

public sealed class CaseSubmittedNotificationHandlerTests : CaseHandlerTestBase
{
    [Fact]
    public async Task Handle_WithValidEvent_CreatesNotificationRecord()
    {
        var caseId = Guid.NewGuid();
        var writer = CreateNotificationWriter();
        var handler = new CaseSubmittedNotificationHandler(writer);
        var evt = new CaseSubmittedNotification(caseId, DateTime.UtcNow);

        await handler.Handle(evt, CancellationToken.None);

        await writer.Received(1).WriteAsync(
            Arg.Is<Notification>(n => n.CaseId == caseId && n.Type == NotificationType.CaseSubmitted),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_DoesNotCallSaveChanges()
    {
        var writer = CreateNotificationWriter();
        var handler = new CaseSubmittedNotificationHandler(writer);
        var evt = new CaseSubmittedNotification(Guid.NewGuid(), DateTime.UtcNow);

        await handler.Handle(evt, CancellationToken.None);

        await writer.Received(1).WriteAsync(Arg.Any<Notification>(), Arg.Any<CancellationToken>());
    }
}
