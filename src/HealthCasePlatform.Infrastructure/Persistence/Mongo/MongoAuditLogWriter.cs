using HealthCasePlatform.Domain.Cases;
using MongoDB.Driver;

namespace HealthCasePlatform.Infrastructure.Persistence.Mongo;

public sealed class MongoAuditLogWriter : IAuditLogWriter
{
    private readonly IMongoCollection<AuditEntry> _collection;

    public MongoAuditLogWriter(IMongoDatabase database, string collectionName)
    {
        _collection = database.GetCollection<AuditEntry>(collectionName);
    }

    public async Task WriteAsync(AuditEntry entry, CancellationToken cancellationToken)
        => await _collection.InsertOneAsync(entry, cancellationToken: cancellationToken);

    public async Task<IReadOnlyList<AuditEntry>> GetByCaseIdAsync(Guid caseId, CancellationToken cancellationToken)
        => await _collection
            .Find(x => x.CaseId == caseId)
            .Sort(Builders<AuditEntry>.Sort.Ascending(x => x.OccurredAt).Ascending(x => x.Id))
            .ToListAsync(cancellationToken);
}
