using HealthCasePlatform.Domain.Enums;

namespace HealthCasePlatform.Domain.Cases;

public interface IAuditLogWriter
{
    Task WriteAsync(AuditEntry entry, CancellationToken cancellationToken);

    Task<IReadOnlyList<AuditEntry>> GetByCaseIdAsync(Guid caseId, CancellationToken cancellationToken);
}
