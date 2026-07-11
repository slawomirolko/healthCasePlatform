using ErrorOr;
using HealthCasePlatform.Domain.Cases;
using Mediator;

namespace HealthCasePlatform.Application.Cases.Commands;

public sealed class AssignLegalReviewerCommandHandler : ICommandHandler<AssignLegalReviewerCommand, ErrorOr<RegulatoryCase>>
{
    private readonly ICaseRepository _repository;

    public AssignLegalReviewerCommandHandler(ICaseRepository repository)
    {
        _repository = repository;
    }

    public ValueTask<ErrorOr<RegulatoryCase>> Handle(AssignLegalReviewerCommand command, CancellationToken cancellationToken)
        => CaseTransitionHelper.TransitionAsync(_repository, command.Id, c => c.AssignLegalReviewer(command.ReviewerId), cancellationToken);
}
