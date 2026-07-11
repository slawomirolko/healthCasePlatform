using HealthCasePlatform.Domain.Cases;
using HealthCasePlatform.Domain.Enums;
using HealthCasePlatform.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Shouldly;

namespace HealthCasePlatform.Infrastructure.Tests.Integration.Persistence;

public sealed class NotificationTransactionTests : IClassFixture<DbFixture>
{
    private readonly DbFixture _fixture;

    public NotificationTransactionTests(DbFixture fixture)
    {
        _fixture = fixture;
    }

    private AppDbContext CreateContext() => _fixture.CreateContext();

    private static RegulatoryCase CreateCase() =>
        RegulatoryCase.Create("Title", "Description", Guid.CreateVersion7(), CasePriority.High, "creator", "PL").Value;

    [Fact]
    public async Task SaveChanges_CaseAndNotification_PersistedTogether()
    {
        var regulatoryCase = CreateCase();
        var notification = Notification.Create(regulatoryCase.Id, NotificationType.CaseSubmitted).Value;

        await using (var write = CreateContext())
        {
            await write.RegulatoryCases.AddAsync(regulatoryCase);
            await write.Notifications.AddAsync(notification);
            await write.SaveChangesAsync();
        }

        await using var read = CreateContext();
        var loadedCase = await read.RegulatoryCases.FindAsync(regulatoryCase.Id);
        var loadedNotification = await read.Notifications.FindAsync(notification.Id);

        loadedCase.ShouldNotBeNull();
        loadedNotification.ShouldNotBeNull();
        loadedNotification.CaseId.ShouldBe(regulatoryCase.Id);
        loadedNotification.Type.ShouldBe(NotificationType.CaseSubmitted);
    }

    [Fact]
    public async Task SaveChanges_CaseAndNotification_RollBackTogether()
    {
        var regulatoryCase = CreateCase();
        var notification = Notification.Create(regulatoryCase.Id, NotificationType.CaseSubmitted).Value;

        await using (var write = CreateContext())
        {
            await write.Database.BeginTransactionAsync();
            await write.RegulatoryCases.AddAsync(regulatoryCase);
            await write.Notifications.AddAsync(notification);
            await write.SaveChangesAsync();
            await write.Database.RollbackTransactionAsync();
        }

        await using var read = CreateContext();
        var loadedCase = await read.RegulatoryCases.FindAsync(regulatoryCase.Id);
        var loadedNotification = await read.Notifications.FindAsync(notification.Id);

        loadedCase.ShouldBeNull();
        loadedNotification.ShouldBeNull();
    }
}
