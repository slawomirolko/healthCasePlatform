using HealthCasePlatform.Domain.Enums;

namespace HealthCasePlatform.Domain.Cases;

public interface ICaseRepository
{
    Task<RegulatoryCase?> FindByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<RegulatoryCase?> FindByIdReadOnlyAsync(Guid id, CancellationToken cancellationToken);

    Task AddAsync(RegulatoryCase entity, CancellationToken cancellationToken);

    Task<int> SaveChangesAsync(CancellationToken cancellationToken);

    Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken);

    Task<IReadOnlyList<CaseStatusHistory>> GetHistoryByCaseIdAsync(Guid caseId, CancellationToken cancellationToken);

    Task<(IReadOnlyList<RegulatoryCase> Items, int TotalCount)> ListAsync(
        CaseStatus? status,
        CasePriority? priority,
        string? country,
        int page,
        int pageSize,
        CancellationToken cancellationToken);
}
