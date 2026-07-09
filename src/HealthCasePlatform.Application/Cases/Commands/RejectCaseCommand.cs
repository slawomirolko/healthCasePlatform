using ErrorOr;
using HealthCasePlatform.Domain.Cases;
using Mediator;

namespace HealthCasePlatform.Application.Cases.Commands;

public sealed record RejectCaseCommand(Guid Id) : ICommand<ErrorOr<RegulatoryCase>>;
