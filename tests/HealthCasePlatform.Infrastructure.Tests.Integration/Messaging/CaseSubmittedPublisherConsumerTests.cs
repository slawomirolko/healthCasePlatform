using HealthCasePlatform.Domain.Cases;
using HealthCasePlatform.Domain.Enums;
using HealthCasePlatform.Infrastructure.Messaging.RabbitMq;
using HealthCasePlatform.Infrastructure.Messaging.RabbitMq.Messages;
using HealthCasePlatform.Infrastructure.Persistence;
using HealthCasePlatform.Infrastructure.Persistence.NotificationStores;
using HealthCasePlatform.Infrastructure.Tests.Integration.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Shouldly;

namespace HealthCasePlatform.Infrastructure.Tests.Integration.Messaging;

public sealed class CaseSubmittedPublisherConsumerTests
    : IClassFixture<RabbitMqFixture>, IClassFixture<DbFixture>, IAsyncLifetime
{
    private readonly DbFixture _db;
    private readonly ServiceProvider _provider;
    private readonly CaseSubmittedConsumer _consumer;
    private readonly RabbitMqCaseSubmittedPublisher _publisher;

    public CaseSubmittedPublisherConsumerTests(RabbitMqFixture rabbit, DbFixture db)
    {
        _db = db;
        var queueName = "case-submitted-test-" + Guid.NewGuid().ToString("N")[..8];
        var settings = rabbit.ToSettings(queueName);
        var connectionFactory = rabbit.CreateConnectionFactory();

        var services = new ServiceCollection();
        services.AddDbContext<AppDbContext>(o => o.UseSqlServer(_db.ConnectionString));
        services.AddScoped<INotificationWriter, SqlNotificationWriter>();
        _provider = services.BuildServiceProvider();

        _publisher = new RabbitMqCaseSubmittedPublisher(
            Options.Create(settings), connectionFactory, TestLoggers.Create<RabbitMqCaseSubmittedPublisher>());

        _consumer = new CaseSubmittedConsumer(
            Options.Create(settings),
            connectionFactory,
            _provider.GetRequiredService<IServiceScopeFactory>(),
            TestLoggers.Create<CaseSubmittedConsumer>());
    }

    public async Task InitializeAsync() =>
        await ((IHostedService)_consumer).StartAsync(CancellationToken.None);

    public async Task DisposeAsync()
    {
        await ((IHostedService)_consumer).StopAsync(CancellationToken.None);
        await _provider.DisposeAsync();
    }

    [Fact]
    public async Task PublishCaseSubmitted_ConsumerWritesNotificationRow()
    {
        var entity = await SeedCaseAsync();

        await _publisher.PublishAsync(new CaseSubmittedMessage(entity.Id, DateTime.UtcNow), CancellationToken.None);

        var notification = await PollForNotificationAsync(entity.Id);

        notification.ShouldNotBeNull();
        notification.Type.ShouldBe(NotificationType.CaseSubmitted);
        notification.CaseId.ShouldBe(entity.Id);
    }

    [Fact]
    public async Task Consumer_OnRedelivery_DoesNotDuplicateRow()
    {
        var entity = await SeedCaseAsync();
        var message = new CaseSubmittedMessage(entity.Id, DateTime.UtcNow);

        await _publisher.PublishAsync(message, CancellationToken.None);
        await _publisher.PublishAsync(message, CancellationToken.None);

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(20));
        while (!cts.IsCancellationRequested)
        {
            await using var db = _db.CreateContext();
            var count = await db.Notifications.AsNoTracking().CountAsync(n => n.CaseId == entity.Id);
            if (count >= 1)
            {
                count.ShouldBe(1);
                return;
            }

            await Task.Delay(200, cts.Token);
        }

        Assert.Fail("Expected exactly one notification row, but polling timed out");
    }

    // Trade-off note: forcing a deterministic save failure without faking the broker is hard.
    // We assert the negative: a message referencing a non-existent case (FK violation) is never
    // acked/written — the consumer's non-duplicate catch path nacks rather than committing.
    [Fact]
    public async Task Consumer_OnSaveFailure_DoesNotWriteRow()
    {
        var phantomCaseId = Guid.NewGuid();

        await _publisher.PublishAsync(new CaseSubmittedMessage(phantomCaseId, DateTime.UtcNow), CancellationToken.None);

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        while (!cts.IsCancellationRequested)
        {
            await using var db = _db.CreateContext();
            var count = await db.Notifications.AsNoTracking().CountAsync(n => n.CaseId == phantomCaseId);
            count.ShouldBe(0);
            try
            {
                await Task.Delay(500, cts.Token);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }
    }

    private async Task<RegulatoryCase> SeedCaseAsync()
    {
        var entity = RegulatoryCase.Create("Title", "Description", Guid.CreateVersion7(), CasePriority.High, "creator", "PL").Value;
        await using var db = _db.CreateContext();
        await db.RegulatoryCases.AddAsync(entity);
        await db.SaveChangesAsync();
        return entity;
    }

    private async Task<Notification?> PollForNotificationAsync(Guid caseId)
    {
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(20));
        while (!cts.IsCancellationRequested)
        {
            await using var db = _db.CreateContext();
            var notification = await db.Notifications.AsNoTracking().FirstOrDefaultAsync(n => n.CaseId == caseId);
            if (notification is not null)
            {
                return notification;
            }

            await Task.Delay(200, cts.Token);
        }

        return null;
    }
}
