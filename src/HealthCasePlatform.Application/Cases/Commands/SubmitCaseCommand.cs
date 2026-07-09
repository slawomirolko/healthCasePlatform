using ErrorOr;
using HealthCasePlatform.Domain.Cases;
using Mediator;

namespace HealthCasePlatform.Application.Cases.Commands;

public sealed record SubmitCaseCommand(Guid Id) : ICommand<ErrorOr<RegulatoryCase>>;
