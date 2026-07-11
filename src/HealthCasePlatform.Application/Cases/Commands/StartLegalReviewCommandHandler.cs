using ErrorOr;
using HealthCasePlatform.Domain.Cases;
using HealthCasePlatform.Domain.Enums;
using Mediator;

namespace HealthCasePlatform.Application.Cases.Commands;

public sealed class StartLegalReviewCommandHandler : ICommandHandler<StartLegalReviewCommand, ErrorOr<RegulatoryCase>>
{
    private readonly ICaseRepository _repository;
    private readonly IAuditLogWriter _writer;

    public StartLegalReviewCommandHandler(ICaseRepository repository, IAuditLogWriter writer)
    {
        _repository = repository;
        _writer = writer;
    }

    public ValueTask<ErrorOr<RegulatoryCase>> Handle(StartLegalReviewCommand command, CancellationToken cancellationToken)
        => CaseTransitionHelper.TransitionAsync(_repository, _writer, command.Id, c => c.StartLegalReview(), cancellationToken, command.Actor, AuditAction.StatusChanged);
}
