using ErrorOr;
using HealthCasePlatform.Domain.Cases;
using HealthCasePlatform.Domain.Cases.Events;
using HealthCasePlatform.Domain.Cases.Messaging;
using HealthCasePlatform.Domain.Enums;
using Mediator;

namespace HealthCasePlatform.Application.Cases.Commands;

public sealed class SubmitCaseCommandHandler : ICommandHandler<SubmitCaseCommand, ErrorOr<RegulatoryCase>>
{
    private readonly ICaseRepository _repository;
    private readonly IAuditLogWriter _auditWriter;
    private readonly ICaseEventOutbox _outbox;

    public SubmitCaseCommandHandler(ICaseRepository repository, IAuditLogWriter auditWriter, ICaseEventOutbox outbox)
    {
        _repository = repository;
        _auditWriter = auditWriter;
        _outbox = outbox;
    }

    public async ValueTask<ErrorOr<RegulatoryCase>> Handle(SubmitCaseCommand command, CancellationToken cancellationToken)
    {
        var entity = await _repository.FindByIdAsync(command.Id, cancellationToken);
        if (entity is null)
        {
            return CaseErrors.NotFound;
        }

        var fromStatus = entity.Status;
        var result = entity.Submit();
        if (result.IsError)
        {
            return result.Errors;
        }

        var toStatus = entity.Status;
        var audit = AuditEntry.Create(entity.Id, AuditAction.StatusChanged, command.Actor!, $"{fromStatus} → {toStatus}");
        await _auditWriter.WriteAsync(audit.Value, cancellationToken);

        await DispatchDomainEventsAsync(entity, cancellationToken);

        await _repository.SaveChangesAsync(cancellationToken);
        return entity;
    }

    private async Task DispatchDomainEventsAsync(RegulatoryCase entity, CancellationToken cancellationToken)
    {
        foreach (var domainEvent in entity.DomainEvents)
        {
            if (domainEvent is CaseSubmitted cs)
            {
                await _outbox.EnqueueCaseSubmittedAsync(cs.CaseId, cs.OccurredAtUtc, cancellationToken);
            }
        }

        entity.ClearDomainEvents();
    }
}
