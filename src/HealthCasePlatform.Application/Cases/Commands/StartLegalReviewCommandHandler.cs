using ErrorOr;
using HealthCasePlatform.Domain.Cases;
using Mediator;

namespace HealthCasePlatform.Application.Cases.Commands;

public sealed class StartLegalReviewCommandHandler : ICommandHandler<StartLegalReviewCommand, ErrorOr<RegulatoryCase>>
{
    private readonly ICaseRepository _repository;

    public StartLegalReviewCommandHandler(ICaseRepository repository)
    {
        _repository = repository;
    }

    public ValueTask<ErrorOr<RegulatoryCase>> Handle(StartLegalReviewCommand command, CancellationToken cancellationToken)
        => CaseTransitionHelper.TransitionAsync(_repository, command.Id, c => c.StartLegalReview(), cancellationToken);
}
