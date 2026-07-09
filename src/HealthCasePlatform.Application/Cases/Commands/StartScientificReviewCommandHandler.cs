using ErrorOr;
using HealthCasePlatform.Domain.Cases;
using Mediator;

namespace HealthCasePlatform.Application.Cases.Commands;

public sealed class StartScientificReviewCommandHandler : ICommandHandler<StartScientificReviewCommand, ErrorOr<RegulatoryCase>>
{
    private readonly ICaseRepository _repository;

    public StartScientificReviewCommandHandler(ICaseRepository repository)
    {
        _repository = repository;
    }

    public ValueTask<ErrorOr<RegulatoryCase>> Handle(StartScientificReviewCommand command, CancellationToken cancellationToken)
        => CaseTransitionHelper.TransitionAsync(_repository, command.Id, c => c.StartScientificReview(), cancellationToken);
}
