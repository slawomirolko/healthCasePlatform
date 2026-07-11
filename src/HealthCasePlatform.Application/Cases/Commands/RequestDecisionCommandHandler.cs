using ErrorOr;
using HealthCasePlatform.Domain.Cases;
using HealthCasePlatform.Domain.Enums;
using Mediator;

namespace HealthCasePlatform.Application.Cases.Commands;

public sealed class RequestDecisionCommandHandler : ICommandHandler<RequestDecisionCommand, ErrorOr<RegulatoryCase>>
{
    private readonly ICaseRepository _repository;

    public RequestDecisionCommandHandler(ICaseRepository repository)
    {
        _repository = repository;
    }

    public ValueTask<ErrorOr<RegulatoryCase>> Handle(RequestDecisionCommand command, CancellationToken cancellationToken)
        => CaseTransitionHelper.TransitionAsync(_repository, command.Id, c => c.RequestDecision(), cancellationToken, command.Actor, AuditAction.StatusChanged);
}
