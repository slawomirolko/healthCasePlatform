using HealthCasePlatform.Domain.Cases;
using HealthCasePlatform.Infrastructure.Persistence;
using Microsoft.AspNetCore.Http.HttpResults;

namespace HealthCasePlatform.Api.Cases;

public static class CasesEndpoints
{
    public static RouteGroupBuilder MapCasesEndpoints(this RouteGroupBuilder group)
    {
        group.MapPost("/cases", CreateCase)
            .WithName("CreateCase");

        return group;
    }

    private static async Task<Results<Created<CreateCaseResponse>, ValidationProblem>> CreateCase(
        CreateCaseRequest request,
        AppDbContext db,
        CancellationToken ct)
    {
        var result = RegulatoryCase.Create(
            request.Title,
            request.Description ?? string.Empty,
            request.CaseTypeId,
            request.Priority,
            request.CreatedBy);

        if (result.IsError)
        {
            var errors = result.Errors.ToDictionary(e => e.Code, e => new[] { e.Description });
            return TypedResults.ValidationProblem(errors);
        }

        var entity = result.Value;
        db.RegulatoryCases.Add(entity);
        await db.SaveChangesAsync(ct);

        var response = new CreateCaseResponse(
            entity.Id,
            entity.Title,
            entity.Description,
            entity.CaseTypeId,
            entity.Status.ToString(),
            entity.Priority.ToString(),
            entity.CreatedBy,
            entity.CreatedAt);

        return TypedResults.Created($"/api/v1/cases/{entity.Id}", response);
    }
}
