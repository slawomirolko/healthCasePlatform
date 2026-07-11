using ErrorOr;
using HealthCasePlatform.Domain.Cases;
using HealthCasePlatform.Domain.Enums;
using Mediator;

namespace HealthCasePlatform.Application.Cases.Commands;

public sealed class ApproveCaseCommandHandler : ICommandHandler<ApproveCaseCommand, ErrorOr<RegulatoryCase>>
{
    private readonly ICaseRepository _repository;

    public ApproveCaseCommandHandler(ICaseRepository repository)
    {
        _repository = repository;
    }

    public ValueTask<ErrorOr<RegulatoryCase>> Handle(ApproveCaseCommand command, CancellationToken cancellationToken)
        => CaseTransitionHelper.TransitionAsync(_repository, command.Id, c => c.Approve(), cancellationToken, command.Actor, AuditAction.DecisionMade);
}
