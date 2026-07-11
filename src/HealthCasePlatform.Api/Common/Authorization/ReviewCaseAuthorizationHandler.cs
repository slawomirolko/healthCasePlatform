using System.Security.Claims;
using HealthCasePlatform.Domain.Cases;
using Microsoft.AspNetCore.Authorization;

namespace HealthCasePlatform.Api.Common.Authorization;

public sealed class ReviewCaseAuthorizationHandler : AuthorizationHandler<IAuthorizationRequirement>
{
    protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, IAuthorizationRequirement requirement)
    {
        if (context.User.IsInRole(AppRoles.TeamLeader))
        {
            context.Succeed(requirement);
            return Task.CompletedTask;
        }

        if (context.Resource is not RegulatoryCase entity)
        {
            return Task.CompletedTask;
        }

        var userId = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                     ?? context.User.Identity?.Name;

        if (string.IsNullOrEmpty(userId))
        {
            return Task.CompletedTask;
        }

        var assigned = requirement switch
        {
            ReviewScientificRequirement => entity.AssignedScientificReviewerId,
            ReviewLegalRequirement => entity.AssignedLegalReviewerId,
            _ => null
        };

        if (assigned == userId)
        {
            context.Succeed(requirement);
        }

        return Task.CompletedTask;
    }
}
