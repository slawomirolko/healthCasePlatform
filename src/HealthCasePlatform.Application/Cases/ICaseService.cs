using ErrorOr;
using HealthCasePlatform.Application.Cases.Models;
using HealthCasePlatform.Domain.Cases;
using HealthCasePlatform.Domain.Enums;

namespace HealthCasePlatform.Application.Cases;

public interface ICaseService
{
    Task<ErrorOr<RegulatoryCase>> CreateAsync(
        string title,
        string description,
        Guid caseTypeId,
        CasePriority priority,
        string createdBy,
        string country,
        CancellationToken cancellationToken);

    Task<RegulatoryCase?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<PagedResult<RegulatoryCase>> ListAsync(
        CaseListFilter filter,
        int page,
        int pageSize,
        CancellationToken cancellationToken);

    Task<ErrorOr<RegulatoryCase>> SubmitAsync(Guid id, CancellationToken cancellationToken);

    Task<ErrorOr<RegulatoryCase>> StartScientificReviewAsync(Guid id, CancellationToken cancellationToken);

    Task<ErrorOr<RegulatoryCase>> StartLegalReviewAsync(Guid id, CancellationToken cancellationToken);

    Task<ErrorOr<RegulatoryCase>> RequestDecisionAsync(Guid id, CancellationToken cancellationToken);

    Task<ErrorOr<RegulatoryCase>> ApproveAsync(Guid id, CancellationToken cancellationToken);

    Task<ErrorOr<RegulatoryCase>> RejectAsync(Guid id, CancellationToken cancellationToken);

    Task<ErrorOr<IReadOnlyList<CaseStatusHistory>>> GetHistoryAsync(Guid id, CancellationToken cancellationToken);
}
