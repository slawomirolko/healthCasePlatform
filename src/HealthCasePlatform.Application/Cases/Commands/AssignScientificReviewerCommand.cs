using ErrorOr;
using HealthCasePlatform.Domain.Cases;
using Mediator;

namespace HealthCasePlatform.Application.Cases.Commands;

public sealed record AssignScientificReviewerCommand(Guid Id, string ReviewerId) : ICommand<ErrorOr<RegulatoryCase>>;
