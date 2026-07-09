using ErrorOr;
using HealthCasePlatform.Domain.Cases;
using Mediator;

namespace HealthCasePlatform.Application.Cases.Commands;

public sealed class CreateCaseCommandHandler : ICommandHandler<CreateCaseCommand, ErrorOr<RegulatoryCase>>
{
    private readonly ICaseRepository _repository;

    public CreateCaseCommandHandler(ICaseRepository repository)
    {
        _repository = repository;
    }

    public async ValueTask<ErrorOr<RegulatoryCase>> Handle(CreateCaseCommand command, CancellationToken cancellationToken)
    {
        var result = RegulatoryCase.Create(
            command.Title,
            command.Description,
            command.CaseTypeId,
            command.Priority,
            command.CreatedBy,
            command.Country);

        if (result.IsError)
        {
            return result.Errors;
        }

        var entity = result.Value;
        await _repository.AddAsync(entity, cancellationToken);
        await _repository.SaveChangesAsync(cancellationToken);
        return entity;
    }
}
