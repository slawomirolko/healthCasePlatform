using ErrorOr;
using HealthCasePlatform.Domain.Cases;
using Mediator;

namespace HealthCasePlatform.Application.Cases.Commands;

public sealed class SubmitCaseCommandHandler : ICommandHandler<SubmitCaseCommand, ErrorOr<RegulatoryCase>>
{
    private readonly ICaseRepository _repository;

    public SubmitCaseCommandHandler(ICaseRepository repository)
    {
        _repository = repository;
    }

    public ValueTask<ErrorOr<RegulatoryCase>> Handle(SubmitCaseCommand command, CancellationToken cancellationToken)
        => CaseTransitionHelper.TransitionAsync(_repository, command.Id, c => c.Submit(), cancellationToken);
}
