using HealthCasePlatform.Application.Cases.Queries;
using HealthCasePlatform.Domain.Cases;
using Mediator;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;

namespace HealthCasePlatform.Api.Common.Authorization;

public sealed class ReviewAuthorizationFilter : IEndpointFilter
{
    private readonly IAuthorizationRequirement _requirement;

    private ReviewAuthorizationFilter(IAuthorizationRequirement requirement)
    {
        _requirement = requirement;
    }

    public static ReviewAuthorizationFilter For(IAuthorizationRequirement requirement) => new(requirement);

    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        var httpContext = context.HttpContext;
        var mediator = httpContext.RequestServices.GetRequiredService<IMediator>();
        var authorizationService = httpContext.RequestServices.GetRequiredService<IAuthorizationService>();

        if (httpContext.GetRouteValue("id") is not string routeValue
            || !Guid.TryParse(routeValue, out var id))
        {
            return TypedResults.NotFound();
        }

        var entity = await mediator.Send(new GetCaseQuery(id), httpContext.RequestAborted);
        if (entity is null)
        {
            return TypedResults.NotFound();
        }

        var authorizationResult = await authorizationService.AuthorizeAsync(httpContext.User, entity, _requirement);
        if (!authorizationResult.Succeeded)
        {
            return Results.StatusCode(StatusCodes.Status403Forbidden);
        }

        return await next(context);
    }
}
