using HealthCasePlatform.Domain.Cases;
using HealthCasePlatform.Domain.Cases.Messaging;
using HealthCasePlatform.Domain.Enums;
using HealthCasePlatform.Infrastructure.Messaging.RabbitMq;
using HealthCasePlatform.Infrastructure.Messaging.RabbitMq.Outbox;
using HealthCasePlatform.Infrastructure.Persistence;
using HealthCasePlatform.Infrastructure.Tests.Integration.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using Shouldly;

namespace HealthCasePlatform.Infrastructure.Tests.Integration.Messaging;

public sealed class OutboxResilienceTests : IClassFixture<DbFixture>, IAsyncLifetime
{
    private readonly DbFixture _db;
    private readonly OutboxDispatcher _dispatcher;
    private readonly ServiceProvider _provider;

    public OutboxResilienceTests(DbFixture db)
    {
        _db = db;
        var settings = new RabbitMqSettings
        {
            Host = "localhost",
            Port = 1,
            UserName = "guest",
            Password = "guest",
            VirtualHost = "/",
            CaseSubmittedQueue = "case-submitted-resilience",
            DispatcherIntervalSeconds = 1
        };

        var connectionFactory = new ConnectionFactory
        {
            HostName = settings.Host,
            Port = settings.Port,
            UserName = settings.UserName,
            Password = settings.Password,
            VirtualHost = settings.VirtualHost
        };

        var services = new ServiceCollection();
        services.AddDbContext<AppDbContext>(o => o.UseSqlServer(_db.ConnectionString));
        _provider = services.BuildServiceProvider();

        _dispatcher = new OutboxDispatcher(
            Options.Create(settings),
            _provider.GetRequiredService<IServiceScopeFactory>(),
            new RabbitMqCaseSubmittedPublisher(
                Options.Create(settings), connectionFactory, TestLoggers.Create<RabbitMqCaseSubmittedPublisher>()),
            TestLoggers.Create<OutboxDispatcher>());
    }

    public async Task InitializeAsync() =>
        await ((IHostedService)_dispatcher).StartAsync(CancellationToken.None);

    public async Task DisposeAsync()
    {
        await ((IHostedService)_dispatcher).StopAsync(CancellationToken.None);
        await _provider.DisposeAsync();
    }

    [Fact]
    public async Task EnqueueCaseSubmitted_WhenBrokerUnreachable_BusinessOpStillCommits()
    {
        var entity = RegulatoryCase.Create("Title", "Description", Guid.CreateVersion7(), CasePriority.High, "creator", "PL").Value;
        entity.Submit().IsError.ShouldBeFalse();

        Guid outboxId;
        await using (var seed = _db.CreateContext())
        {
            var outbox = new SqlCaseEventOutbox(seed);
            await outbox.EnqueueCaseSubmittedAsync(entity.Id, DateTime.UtcNow, CancellationToken.None);
            await seed.RegulatoryCases.AddAsync(entity);
            await seed.SaveChangesAsync();

            var staged = await seed.OutboxMessages.SingleAsync();
            staged.ProcessedAtUtc.ShouldBeNull();
            staged.Attempts.ShouldBe(0);
            outboxId = staged.Id;
        }

        OutboxMessage? relayed = null;
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(8));
        while (!cts.IsCancellationRequested)
        {
            await using var db = _db.CreateContext();
            relayed = await db.OutboxMessages.FindAsync(outboxId);
            if (relayed is { Attempts: > 0 })
            {
                break;
            }

            await Task.Delay(300, cts.Token);
        }

        relayed.ShouldNotBeNull();
        relayed.Attempts.ShouldBeGreaterThan(0);
        relayed.ProcessedAtUtc.ShouldBeNull();
        relayed.LastError.ShouldNotBeNullOrEmpty();
    }
}
