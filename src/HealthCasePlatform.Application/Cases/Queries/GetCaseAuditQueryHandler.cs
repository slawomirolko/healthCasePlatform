using ErrorOr;
using HealthCasePlatform.Domain.Cases;
using Mediator;

namespace HealthCasePlatform.Application.Cases.Queries;

public sealed class GetCaseAuditQueryHandler : IQueryHandler<GetCaseAuditQuery, ErrorOr<IReadOnlyList<AuditEntry>>>
{
    private readonly ICaseRepository _repository;
    private readonly IAuditLogWriter _writer;

    public GetCaseAuditQueryHandler(ICaseRepository repository, IAuditLogWriter writer)
    {
        _repository = repository;
        _writer = writer;
    }

    public async ValueTask<ErrorOr<IReadOnlyList<AuditEntry>>> Handle(GetCaseAuditQuery query, CancellationToken cancellationToken)
    {
        var exists = await _repository.ExistsAsync(query.Id, cancellationToken);
        if (!exists)
        {
            return CaseErrors.NotFound;
        }

        var audit = await _writer.GetByCaseIdAsync(query.Id, cancellationToken);
        return ErrorOrFactory.From(audit);
    }
}
