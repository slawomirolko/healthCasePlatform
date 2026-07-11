using HealthCasePlatform.Domain.Cases;
using HealthCasePlatform.Domain.Enums;
using Mediator;

namespace HealthCasePlatform.Application.Cases.Notifications;

public sealed class CaseSubmittedNotificationHandler : INotificationHandler<CaseSubmittedNotification>
{
    private readonly INotificationWriter _writer;

    public CaseSubmittedNotificationHandler(INotificationWriter writer)
    {
        _writer = writer;
    }

    public async ValueTask Handle(CaseSubmittedNotification evt, CancellationToken cancellationToken)
    {
        var notification = Notification.Create(evt.CaseId, NotificationType.CaseSubmitted);
        await _writer.WriteAsync(notification.Value, cancellationToken);
    }
}
