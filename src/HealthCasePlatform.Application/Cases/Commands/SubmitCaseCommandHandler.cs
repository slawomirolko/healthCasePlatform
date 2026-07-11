using ErrorOr;
using HealthCasePlatform.Domain.Cases;
using HealthCasePlatform.Domain.Enums;
using Mediator;

namespace HealthCasePlatform.Application.Cases.Commands;

public sealed class SubmitCaseCommandHandler : ICommandHandler<SubmitCaseCommand, ErrorOr<RegulatoryCase>>
{
    private readonly ICaseRepository _repository;
    private readonly IAuditLogWriter _writer;

    public SubmitCaseCommandHandler(ICaseRepository repository, IAuditLogWriter writer)
    {
        _repository = repository;
        _writer = writer;
    }

    public ValueTask<ErrorOr<RegulatoryCase>> Handle(SubmitCaseCommand command, CancellationToken cancellationToken)
        => CaseTransitionHelper.TransitionAsync(_repository, _writer, command.Id, c => c.Submit(), cancellationToken, command.Actor, AuditAction.StatusChanged);
}
