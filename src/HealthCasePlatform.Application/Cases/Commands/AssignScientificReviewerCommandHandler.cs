using ErrorOr;
using HealthCasePlatform.Domain.Cases;
using Mediator;

namespace HealthCasePlatform.Application.Cases.Commands;

public sealed class AssignScientificReviewerCommandHandler : ICommandHandler<AssignScientificReviewerCommand, ErrorOr<RegulatoryCase>>
{
    private readonly ICaseRepository _repository;

    public AssignScientificReviewerCommandHandler(ICaseRepository repository)
    {
        _repository = repository;
    }

    public ValueTask<ErrorOr<RegulatoryCase>> Handle(AssignScientificReviewerCommand command, CancellationToken cancellationToken)
        => CaseTransitionHelper.TransitionAsync(_repository, command.Id, c => c.AssignScientificReviewer(command.ReviewerId), cancellationToken);
}
