using ErrorOr;
using HealthCasePlatform.Domain.Cases;
using Mediator;

namespace HealthCasePlatform.Application.Cases.Commands;

public sealed record ApproveCaseCommand(Guid Id, string? Actor = null) : ICommand<ErrorOr<RegulatoryCase>>;
