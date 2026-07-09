using HealthCasePlatform.Application.Cases.Models;
using HealthCasePlatform.Domain.Cases;
using Mediator;

namespace HealthCasePlatform.Application.Cases.Queries;

public sealed class ListCasesQueryHandler : IQueryHandler<ListCasesQuery, PagedResult<RegulatoryCase>>
{
    private readonly ICaseRepository _repository;

    public ListCasesQueryHandler(ICaseRepository repository)
    {
        _repository = repository;
    }

    public async ValueTask<PagedResult<RegulatoryCase>> Handle(ListCasesQuery query, CancellationToken cancellationToken)
    {
        var page = Math.Max(1, query.Page);
        var pageSize = Math.Clamp(query.PageSize, 1, 100);
        var country = string.IsNullOrWhiteSpace(query.Country)
            ? null
            : query.Country.Trim().ToUpperInvariant();

        var (items, totalCount) = await _repository.ListAsync(
            query.Status,
            query.Priority,
            country,
            page,
            pageSize,
            cancellationToken);

        var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

        return new PagedResult<RegulatoryCase>(items, page, pageSize, totalCount, totalPages);
    }
}
