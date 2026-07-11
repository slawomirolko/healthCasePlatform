using ErrorOr;
using HealthCasePlatform.Domain.Cases;
using HealthCasePlatform.Domain.Enums;
using Mediator;

namespace HealthCasePlatform.Application.Cases.Commands;

public sealed class CreateCaseCommandHandler : ICommandHandler<CreateCaseCommand, ErrorOr<RegulatoryCase>>
{
    private readonly ICaseRepository _repository;
    private readonly IAuditLogWriter _writer;

    public CreateCaseCommandHandler(ICaseRepository repository, IAuditLogWriter writer)
    {
        _repository = repository;
        _writer = writer;
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
        var audit = AuditEntry.Create(entity.Id, AuditAction.CaseCreated, command.CreatedBy, entity.Title);
        await _writer.WriteAsync(audit.Value, cancellationToken);
        await _repository.SaveChangesAsync(cancellationToken);
        return entity;
    }
}
