using ErrorOr;
using HealthCasePlatform.Domain.Cases;

namespace HealthCasePlatform.Application.Cases.Commands;

internal static class CaseTransitionHelper
{
    public static async ValueTask<ErrorOr<RegulatoryCase>> TransitionAsync(
        ICaseRepository repository,
        Guid id,
        Func<RegulatoryCase, ErrorOr<Success>> transition,
        CancellationToken cancellationToken)
    {
        var entity = await repository.FindByIdAsync(id, cancellationToken);
        if (entity is null)
        {
            return CaseErrors.NotFound;
        }

        var result = transition(entity);
        if (result.IsError)
        {
            return result.Errors;
        }

        await repository.SaveChangesAsync(cancellationToken);
        return entity;
    }
}
