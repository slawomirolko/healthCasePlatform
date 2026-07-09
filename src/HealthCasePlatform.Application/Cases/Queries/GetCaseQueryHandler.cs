using HealthCasePlatform.Domain.Cases;
using Mediator;

namespace HealthCasePlatform.Application.Cases.Queries;

public sealed class GetCaseQueryHandler : IQueryHandler<GetCaseQuery, RegulatoryCase?>
{
    private readonly ICaseRepository _repository;

    public GetCaseQueryHandler(ICaseRepository repository)
    {
        _repository = repository;
    }

    public async ValueTask<RegulatoryCase?> Handle(GetCaseQuery query, CancellationToken cancellationToken)
        => await _repository.FindByIdReadOnlyAsync(query.Id, cancellationToken);
}
