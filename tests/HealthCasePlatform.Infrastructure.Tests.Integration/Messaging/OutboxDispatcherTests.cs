using HealthCasePlatform.Domain.Cases.Messaging;
using HealthCasePlatform.Infrastructure.Messaging.RabbitMq;
using HealthCasePlatform.Infrastructure.Messaging.RabbitMq.Outbox;
using HealthCasePlatform.Infrastructure.Persistence;
using HealthCasePlatform.Infrastructure.Tests.Integration.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Shouldly;

namespace HealthCasePlatform.Infrastructure.Tests.Integration.Messaging;

public sealed class OutboxDispatcherTests
    : IClassFixture<RabbitMqFixture>, IClassFixture<DbFixture>, IAsyncLifetime
{
    private readonly RabbitMqFixture _rabbit;
    private readonly DbFixture _db;
    private readonly OutboxDispatcher _dispatcher;
    private readonly RabbitMqSettings _settings;

    public OutboxDispatcherTests(RabbitMqFixture rabbit, DbFixture db)
    {
        _rabbit = rabbit;
        _db = db;
        _settings = rabbit.ToSettings("case-submitted-dispatcher-" + Guid.NewGuid().ToString("N")[..8]);
        var connectionFactory = rabbit.CreateConnectionFactory();

        var services = new ServiceCollection();
        services.AddDbContext<AppDbContext>(o => o.UseSqlServer(_db.ConnectionString));
        _dispatcher = new OutboxDispatcher(
            Options.Create(_settings),
            services.BuildServiceProvider().GetRequiredService<IServiceScopeFactory>(),
            new RabbitMqCaseSubmittedPublisher(
                Options.Create(_settings), connectionFactory, TestLoggers.Create<RabbitMqCaseSubmittedPublisher>()),
            TestLoggers.Create<OutboxDispatcher>());
    }

    public async Task InitializeAsync() =>
        await ((IHostedService)_dispatcher).StartAsync(CancellationToken.None);

    public async Task DisposeAsync() =>
        await ((IHostedService)_dispatcher).StopAsync(CancellationToken.None);

    [Fact]
    public async Task Dispatch_RelaysStagedOutboxRowToRabbitMq()
    {
        var caseId = Guid.NewGuid();

        await using (var seed = _db.CreateContext())
        {
            var outbox = new SqlCaseEventOutbox(seed);
            await outbox.EnqueueCaseSubmittedAsync(caseId, DateTime.UtcNow, CancellationToken.None);
            await seed.SaveChangesAsync();
        }

        OutboxMessage? relayed = null;
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(20));
        while (!cts.IsCancellationRequested)
        {
            await using var db = _db.CreateContext();
            relayed = await db.OutboxMessages.AsNoTracking().FirstOrDefaultAsync(o => o.Type == "CaseSubmitted");
            if (relayed is { ProcessedAtUtc: not null })
            {
                break;
            }

            await Task.Delay(200, cts.Token);
        }

        relayed.ShouldNotBeNull();
        relayed.ProcessedAtUtc.ShouldNotBeNull();
        relayed.Attempts.ShouldBe(0);
    }
}
