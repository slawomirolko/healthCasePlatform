using System.Text.Json;
using HealthCasePlatform.Domain.Cases.Messaging;
using HealthCasePlatform.Infrastructure.Messaging.RabbitMq.Messages;
using HealthCasePlatform.Infrastructure.Persistence;

namespace HealthCasePlatform.Infrastructure.Messaging.RabbitMq.Outbox;

internal sealed class SqlCaseEventOutbox : ICaseEventOutbox
{
    private readonly AppDbContext _db;

    public SqlCaseEventOutbox(AppDbContext db)
    {
        _db = db;
    }

    public async Task EnqueueCaseSubmittedAsync(Guid caseId, DateTime occurredAtUtc, CancellationToken cancellationToken)
    {
        var message = new CaseSubmittedMessage(caseId, occurredAtUtc);
        var payload = JsonSerializer.Serialize(message);
        var entry = OutboxMessage.Create("CaseSubmitted", payload);
        await _db.OutboxMessages.AddAsync(entry, cancellationToken);
    }
}
