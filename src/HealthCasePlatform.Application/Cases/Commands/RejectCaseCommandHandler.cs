using ErrorOr;
using HealthCasePlatform.Domain.Cases;
using HealthCasePlatform.Domain.Enums;
using Mediator;

namespace HealthCasePlatform.Application.Cases.Commands;

public sealed class RejectCaseCommandHandler : ICommandHandler<RejectCaseCommand, ErrorOr<RegulatoryCase>>
{
    private readonly ICaseRepository _repository;

    public RejectCaseCommandHandler(ICaseRepository repository)
    {
        _repository = repository;
    }

    public ValueTask<ErrorOr<RegulatoryCase>> Handle(RejectCaseCommand command, CancellationToken cancellationToken)
        => CaseTransitionHelper.TransitionAsync(_repository, command.Id, c => c.Reject(), cancellationToken, command.Actor, AuditAction.DecisionMade);
}
