using HealthCasePlatform.Domain.Cases;

namespace HealthCasePlatform.Infrastructure.Persistence.NotificationStores;

public sealed class SqlNotificationWriter : INotificationWriter
{
    private readonly AppDbContext _db;

    public SqlNotificationWriter(AppDbContext db)
    {
        _db = db;
    }

    public async Task WriteAsync(Notification notification, CancellationToken cancellationToken)
        => await _db.Notifications.AddAsync(notification, cancellationToken);
}
