using ErrorOr;
using HealthCasePlatform.Application.Cases.Models;
using HealthCasePlatform.Domain.Cases;
using HealthCasePlatform.Domain.Enums;

namespace HealthCasePlatform.Application.Cases;

public sealed class CaseService : ICaseService
{
    private static readonly Error CaseNotFound = Error.NotFound("RegulatoryCase.NotFound", "Case was not found.");

    private readonly ICaseRepository _repository;

    public CaseService(ICaseRepository repository)
    {
        _repository = repository;
    }

    public async Task<ErrorOr<RegulatoryCase>> CreateAsync(
        string title,
        string description,
        Guid caseTypeId,
        CasePriority priority,
        string createdBy,
        string country,
        CancellationToken cancellationToken)
    {
        var result = RegulatoryCase.Create(title, description, caseTypeId, priority, createdBy, country);
        if (result.IsError)
        {
            return result.Errors;
        }

        var entity = result.Value;
        await _repository.AddAsync(entity, cancellationToken);
        await _repository.SaveChangesAsync(cancellationToken);
        return entity;
    }

    public Task<RegulatoryCase?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
        => _repository.FindByIdReadOnlyAsync(id, cancellationToken);

    public async Task<PagedResult<RegulatoryCase>> ListAsync(
        CaseListFilter filter,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);
        var country = string.IsNullOrWhiteSpace(filter.Country)
            ? null
            : filter.Country.Trim().ToUpperInvariant();

        var (items, totalCount) = await _repository.ListAsync(
            filter.Status,
            filter.Priority,
            country,
            page,
            pageSize,
            cancellationToken);

        var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

        return new PagedResult<RegulatoryCase>(items, page, pageSize, totalCount, totalPages);
    }

    public Task<ErrorOr<RegulatoryCase>> SubmitAsync(Guid id, CancellationToken cancellationToken)
        => TransitionAsync(id, c => c.Submit(), cancellationToken);

    public Task<ErrorOr<RegulatoryCase>> StartScientificReviewAsync(Guid id, CancellationToken cancellationToken)
        => TransitionAsync(id, c => c.StartScientificReview(), cancellationToken);

    public Task<ErrorOr<RegulatoryCase>> StartLegalReviewAsync(Guid id, CancellationToken cancellationToken)
        => TransitionAsync(id, c => c.StartLegalReview(), cancellationToken);

    public Task<ErrorOr<RegulatoryCase>> RequestDecisionAsync(Guid id, CancellationToken cancellationToken)
        => TransitionAsync(id, c => c.RequestDecision(), cancellationToken);

    public Task<ErrorOr<RegulatoryCase>> ApproveAsync(Guid id, CancellationToken cancellationToken)
        => TransitionAsync(id, c => c.Approve(), cancellationToken);

    public Task<ErrorOr<RegulatoryCase>> RejectAsync(Guid id, CancellationToken cancellationToken)
        => TransitionAsync(id, c => c.Reject(), cancellationToken);

    public async Task<ErrorOr<IReadOnlyList<CaseStatusHistory>>> GetHistoryAsync(Guid id, CancellationToken cancellationToken)
    {
        var exists = await _repository.ExistsAsync(id, cancellationToken);
        if (!exists)
        {
            return CaseNotFound;
        }

        var history = await _repository.GetHistoryByCaseIdAsync(id, cancellationToken);
        return ErrorOrFactory.From(history);
    }

    private async Task<ErrorOr<RegulatoryCase>> TransitionAsync(
        Guid id,
        Func<RegulatoryCase, ErrorOr<Success>> transition,
        CancellationToken cancellationToken)
    {
        var entity = await _repository.FindByIdAsync(id, cancellationToken);
        if (entity is null)
        {
            return CaseNotFound;
        }

        var result = transition(entity);
        if (result.IsError)
        {
            return result.Errors;
        }

        await _repository.SaveChangesAsync(cancellationToken);
        return entity;
    }
}
