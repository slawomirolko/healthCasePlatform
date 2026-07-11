using ErrorOr;

namespace HealthCasePlatform.Domain.Cases;

public static class NotificationErrors
{
    public static readonly Error CaseIdEmpty = Error.Validation("Notification.CaseIdEmpty", "Case ID cannot be empty.");
    public static readonly Error TypeInvalid = Error.Validation("Notification.TypeInvalid", "Notification type is invalid.");
}
