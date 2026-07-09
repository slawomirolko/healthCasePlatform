using ErrorOr;
using HealthCasePlatform.Domain.Cases;
using Mediator;

namespace HealthCasePlatform.Application.Cases.Queries;

public sealed class GetCaseHistoryQueryHandler : IQueryHandler<GetCaseHistoryQuery, ErrorOr<IReadOnlyList<CaseStatusHistory>>>
{
    private readonly ICaseRepository _repository;

    public GetCaseHistoryQueryHandler(ICaseRepository repository)
    {
        _repository = repository;
    }

    public async ValueTask<ErrorOr<IReadOnlyList<CaseStatusHistory>>> Handle(GetCaseHistoryQuery query, CancellationToken cancellationToken)
    {
        var exists = await _repository.ExistsAsync(query.Id, cancellationToken);
        if (!exists)
        {
            return CaseErrors.NotFound;
        }

        var history = await _repository.GetHistoryByCaseIdAsync(query.Id, cancellationToken);
        return ErrorOrFactory.From(history);
    }
}
