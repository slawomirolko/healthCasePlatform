using HealthCasePlatform.Domain.Cases;
using HealthCasePlatform.Infrastructure.Persistence;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

namespace HealthCasePlatform.Api.Cases;

public static class CasesEndpoints
{
    public static RouteGroupBuilder MapCasesEndpoints(this RouteGroupBuilder group)
    {
        group.MapPost("/cases", CreateCase)
            .WithName("CreateCase");

        group.MapGet("/cases/{id:guid}", GetCase)
            .WithName("GetCase");

        return group;
    }

    private static async Task<Results<Ok<CaseResponse>, NotFound>> GetCase(
        Guid id,
        AppDbContext db,
        CancellationToken cancellationToken)
    {
        var entity = await db.RegulatoryCases
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);

        if (entity is null)
        {
            return TypedResults.NotFound();
        }

        var response = new CaseResponse(
            entity.Id,
            entity.Title,
            entity.Description,
            entity.CaseTypeId,
            entity.Status.ToString(),
            entity.Priority.ToString(),
            entity.CreatedBy,
            entity.CreatedAt,
            entity.UpdatedAt);

        return TypedResults.Ok(response);
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
