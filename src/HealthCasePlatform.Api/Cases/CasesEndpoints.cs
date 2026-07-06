using HealthCasePlatform.Api.Common;
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
            .WithName("CreateCase")
            .AddEndpointFilter<ValidationFilter<CreateCaseRequest>>();

        group.MapGet("/cases/{id:guid}", GetCase)
            .WithName("GetCase");

        group.MapGet("/cases", ListCases)
            .WithName("ListCases");

        return group;
    }

    private static async Task<Ok<PagedResponse<CaseListItemResponse>>> ListCases(
        [AsParameters] ListCasesRequest request,
        AppDbContext db,
        CancellationToken cancellationToken)
    {
        var page = Math.Max(1, request.Page);
        var pageSize = Math.Clamp(request.PageSize, 1, 100);
        var country = string.IsNullOrWhiteSpace(request.Country)
            ? null
            : request.Country.Trim().ToUpperInvariant();

        IQueryable<RegulatoryCase> query = db.RegulatoryCases.AsNoTracking();

        if (request.Status.HasValue)
        {
            query = query.Where(c => c.Status == request.Status.Value);
        }

        if (request.Priority.HasValue)
        {
            query = query.Where(c => c.Priority == request.Priority.Value);
        }

        if (country is not null)
        {
            query = query.Where(c => c.Country == country);
        }

        query = query.OrderByDescending(c => c.CreatedAt).ThenByDescending(c => c.Id);

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(c => new CaseListItemResponse(
                c.Id,
                c.Title,
                c.Country,
                c.Status.ToString(),
                c.Priority.ToString(),
                c.CreatedAt))
            .ToListAsync(cancellationToken);

        var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

        return TypedResults.Ok(new PagedResponse<CaseListItemResponse>(items, page, pageSize, totalCount, totalPages));
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
            entity.Country,
            entity.CreatedBy,
            entity.CreatedAt,
            entity.UpdatedAt);

        return TypedResults.Ok(response);
    }

    private static async Task<Results<Created<CreateCaseResponse>, ValidationProblem>> CreateCase(
        CreateCaseRequest request,
        AppDbContext db,
        CancellationToken cancellationToken)
    {
        var result = RegulatoryCase.Create(
            request.Title,
            request.Description ?? string.Empty,
            request.CaseTypeId,
            request.Priority,
            request.CreatedBy,
            request.Country);

        if (result.IsError)
        {
            var errors = result.Errors.ToDictionary(e => e.Code, e => new[] { e.Description });
            return TypedResults.ValidationProblem(errors);
        }

        var entity = result.Value;
        db.RegulatoryCases.Add(entity);
        await db.SaveChangesAsync(cancellationToken);

        var response = new CreateCaseResponse(
            entity.Id,
            entity.Title,
            entity.Description,
            entity.CaseTypeId,
            entity.Status.ToString(),
            entity.Priority.ToString(),
            entity.Country,
            entity.CreatedBy,
            entity.CreatedAt);

        return TypedResults.Created($"/api/v1/cases/{entity.Id}", response);
    }
}
