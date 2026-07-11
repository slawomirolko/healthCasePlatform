using HealthCasePlatform.Domain.Cases;
using HealthCasePlatform.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace HealthCasePlatform.Infrastructure.Persistence.Repositories;

public sealed class CaseRepository : ICaseRepository
{
    private readonly AppDbContext _db;

    public CaseRepository(AppDbContext db)
    {
        _db = db;
    }

    public Task<RegulatoryCase?> FindByIdAsync(Guid id, CancellationToken cancellationToken)
        => _db.RegulatoryCases.FirstOrDefaultAsync(c => c.Id == id, cancellationToken);

    public Task<RegulatoryCase?> FindByIdReadOnlyAsync(Guid id, CancellationToken cancellationToken)
        => _db.RegulatoryCases
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);

    public async Task AddAsync(RegulatoryCase entity, CancellationToken cancellationToken)
        => await _db.RegulatoryCases.AddAsync(entity, cancellationToken);

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken)
        => _db.SaveChangesAsync(cancellationToken);

    public Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken)
        => _db.RegulatoryCases
            .AsNoTracking()
            .AnyAsync(c => c.Id == id, cancellationToken);

    public async Task<IReadOnlyList<CaseStatusHistory>> GetHistoryByCaseIdAsync(Guid caseId, CancellationToken cancellationToken)
        => await _db.CaseStatusHistories
            .AsNoTracking()
            .Where(h => h.CaseId == caseId)
            .OrderBy(h => h.TransitionedAt)
            .ToListAsync(cancellationToken);

    public async Task AddAuditEntryAsync(AuditEntry entry, CancellationToken cancellationToken)
        => await _db.AuditEntries.AddAsync(entry, cancellationToken);

    public async Task<IReadOnlyList<AuditEntry>> GetAuditByCaseIdAsync(Guid caseId, CancellationToken cancellationToken)
        => await _db.AuditEntries
            .AsNoTracking()
            .Where(a => a.CaseId == caseId)
            .OrderBy(a => a.OccurredAt)
            .ThenBy(a => a.Id)
            .ToListAsync(cancellationToken);

    public async Task<(IReadOnlyList<RegulatoryCase> Items, int TotalCount)> ListAsync(
        CaseStatus? status,
        CasePriority? priority,
        string? country,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        IQueryable<RegulatoryCase> query = _db.RegulatoryCases.AsNoTracking();

        if (status.HasValue)
        {
            query = query.Where(c => c.Status == status.Value);
        }

        if (priority.HasValue)
        {
            query = query.Where(c => c.Priority == priority.Value);
        }

        if (country is not null)
        {
            query = query.Where(c => c.Country == country);
        }

        query = query.OrderByDescending(c => c.CreatedAt).ThenByDescending(c => c.Id);

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }
}
