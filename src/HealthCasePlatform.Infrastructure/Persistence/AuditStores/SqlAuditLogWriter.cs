using HealthCasePlatform.Domain.Cases;
using Microsoft.EntityFrameworkCore;

namespace HealthCasePlatform.Infrastructure.Persistence.AuditStores;

public sealed class SqlAuditLogWriter : IAuditLogWriter
{
    private readonly AppDbContext _db;

    public SqlAuditLogWriter(AppDbContext db)
    {
        _db = db;
    }

    public async Task WriteAsync(AuditEntry entry, CancellationToken cancellationToken)
        => await _db.AuditEntries.AddAsync(entry, cancellationToken);

    public async Task<IReadOnlyList<AuditEntry>> GetByCaseIdAsync(Guid caseId, CancellationToken cancellationToken)
        => await _db.AuditEntries
            .AsNoTracking()
            .Where(a => a.CaseId == caseId)
            .OrderBy(a => a.OccurredAt)
            .ThenBy(a => a.Id)
            .ToListAsync(cancellationToken);
}
