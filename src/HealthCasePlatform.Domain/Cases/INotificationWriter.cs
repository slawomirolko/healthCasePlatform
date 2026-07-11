namespace HealthCasePlatform.Domain.Cases;

public interface INotificationWriter
{
    Task WriteAsync(Notification notification, CancellationToken cancellationToken);
}
