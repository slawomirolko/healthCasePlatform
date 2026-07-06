using FluentValidation;
using Microsoft.AspNetCore.Http;

namespace HealthCasePlatform.Api.Common;

public sealed class ValidationFilter<T> : IEndpointFilter where T : class
{
    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        var validator = context.HttpContext.RequestServices.GetService<IValidator<T>>();
        if (validator is null)
        {
            return await next(context);
        }

        var target = context.Arguments.OfType<T>().FirstOrDefault();
        if (target is null)
        {
            return await next(context);
        }

        var validation = await validator.ValidateAsync(target, context.HttpContext.RequestAborted);
        if (validation.IsValid)
        {
            return await next(context);
        }

        var errors = validation.Errors
            .GroupBy(f => f.PropertyName)
            .ToDictionary(g => g.Key, g => g.Select(f => f.ErrorMessage).ToArray());

        return TypedResults.ValidationProblem(errors);
    }
}
