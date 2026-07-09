using ErrorOr;
using HealthCasePlatform.Domain.Cases;
using HealthCasePlatform.Domain.Enums;
using Mediator;

namespace HealthCasePlatform.Application.Cases.Commands;

public sealed record CreateCaseCommand(
    string Title,
    string Description,
    Guid CaseTypeId,
    CasePriority Priority,
    string CreatedBy,
    string Country) : ICommand<ErrorOr<RegulatoryCase>>;
