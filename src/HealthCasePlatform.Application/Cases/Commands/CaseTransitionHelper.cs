using ErrorOr;
using HealthCasePlatform.Domain.Cases;
using HealthCasePlatform.Domain.Enums;

namespace HealthCasePlatform.Application.Cases.Commands;

internal static class CaseTransitionHelper
{
    public static async ValueTask<ErrorOr<RegulatoryCase>> TransitionAsync(
        ICaseRepository repository,
        Guid id,
        Func<RegulatoryCase, ErrorOr<Success>> transition,
        CancellationToken cancellationToken,
        string? actor = null,
        AuditAction? auditAction = null)
    {
        var entity = await repository.FindByIdAsync(id, cancellationToken);
        if (entity is null)
        {
            return CaseErrors.NotFound;
        }

        var fromStatus = entity.Status;
        var result = transition(entity);
        if (result.IsError)
        {
            return result.Errors;
        }

        if (auditAction is not null)
        {
            var toStatus = entity.Status;
            var audit = AuditEntry.Create(entity.Id, auditAction.Value, actor!, $"{fromStatus} → {toStatus}");
            await repository.AddAuditEntryAsync(audit.Value, cancellationToken);
        }

        await repository.SaveChangesAsync(cancellationToken);
        return entity;
    }
}
