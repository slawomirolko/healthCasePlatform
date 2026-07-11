using ErrorOr;
using HealthCasePlatform.Domain.Cases;
using Mediator;

namespace HealthCasePlatform.Application.Cases.Commands;

public sealed class AssignLegalReviewerCommandHandler : ICommandHandler<AssignLegalReviewerCommand, ErrorOr<RegulatoryCase>>
{
    private readonly ICaseRepository _repository;
    private readonly IAuditLogWriter _writer;

    public AssignLegalReviewerCommandHandler(ICaseRepository repository, IAuditLogWriter writer)
    {
        _repository = repository;
        _writer = writer;
    }

    public ValueTask<ErrorOr<RegulatoryCase>> Handle(AssignLegalReviewerCommand command, CancellationToken cancellationToken)
        => CaseTransitionHelper.TransitionAsync(_repository, _writer, command.Id, c => c.AssignLegalReviewer(command.ReviewerId), cancellationToken);
}
