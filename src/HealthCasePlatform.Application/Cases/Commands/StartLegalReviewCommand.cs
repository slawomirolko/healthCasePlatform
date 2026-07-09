using ErrorOr;
using HealthCasePlatform.Domain.Cases;
using Mediator;

namespace HealthCasePlatform.Application.Cases.Commands;

public sealed record StartLegalReviewCommand(Guid Id) : ICommand<ErrorOr<RegulatoryCase>>;
