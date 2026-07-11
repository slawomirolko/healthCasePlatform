using ErrorOr;
using HealthCasePlatform.Domain.Common;
using HealthCasePlatform.Domain.Enums;

namespace HealthCasePlatform.Domain.Cases;

public sealed class Notification : Entity
{
    public Guid CaseId { get; private set; }
    public NotificationType Type { get; private set; }
    public DateTime CreatedAt { get; private set; }

    private Notification() { }

    public static ErrorOr<Notification> Create(Guid caseId, NotificationType type)
    {
        if (caseId == Guid.Empty)
        {
            return NotificationErrors.CaseIdEmpty;
        }

        if (!Enum.IsDefined(type))
        {
            return NotificationErrors.TypeInvalid;
        }

        return new Notification
        {
            Id = Guid.CreateVersion7(),
            CaseId = caseId,
            Type = type,
            CreatedAt = DateTime.UtcNow
        };
    }
}
