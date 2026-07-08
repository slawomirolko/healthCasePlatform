using HealthCasePlatform.Api.Common;
using HealthCasePlatform.Domain.Cases;
using HealthCasePlatform.Infrastructure.Persistence;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using ErrorOr;

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

        group.MapPost("/cases/{id:guid}/submission", SubmitCase)
            .WithName("SubmitCase");

        group.MapPost("/cases/{id:guid}/scientific-review", StartScientificReview)
            .WithName("StartScientificReview");

        group.MapPost("/cases/{id:guid}/legal-review", StartLegalReview)
            .WithName("StartLegalReview");

        group.MapPost("/cases/{id:guid}/decision-request", RequestDecision)
            .WithName("RequestDecision");

        group.MapPost("/cases/{id:guid}/approval", ApproveCase)
            .WithName("ApproveCase");

        group.MapPost("/cases/{id:guid}/rejection", RejectCase)
            .WithName("RejectCase");

        group.MapGet("/cases/{id:guid}/history", GetCaseHistory)
            .WithName("GetCaseHistory");

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

    private static Task<Results<Ok<CaseResponse>, NotFound, IResult>> SubmitCase(
        Guid id,
        AppDbContext db,
        CancellationToken cancellationToken = default)
        => TransitionCase(id, db, c => c.Submit(), cancellationToken);

    private static Task<Results<Ok<CaseResponse>, NotFound, IResult>> StartScientificReview(
        Guid id,
        AppDbContext db,
        CancellationToken cancellationToken = default)
        => TransitionCase(id, db, c => c.StartScientificReview(), cancellationToken);

    private static Task<Results<Ok<CaseResponse>, NotFound, IResult>> StartLegalReview(
        Guid id,
        AppDbContext db,
        CancellationToken cancellationToken = default)
        => TransitionCase(id, db, c => c.StartLegalReview(), cancellationToken);

    private static Task<Results<Ok<CaseResponse>, NotFound, IResult>> RequestDecision(
        Guid id,
        AppDbContext db,
        CancellationToken cancellationToken = default)
        => TransitionCase(id, db, c => c.RequestDecision(), cancellationToken);

    private static Task<Results<Ok<CaseResponse>, NotFound, IResult>> ApproveCase(
        Guid id,
        AppDbContext db,
        CancellationToken cancellationToken = default)
        => TransitionCase(id, db, c => c.Approve(), cancellationToken);

    private static Task<Results<Ok<CaseResponse>, NotFound, IResult>> RejectCase(
        Guid id,
        AppDbContext db,
        CancellationToken cancellationToken = default)
        => TransitionCase(id, db, c => c.Reject(), cancellationToken);

    private static CaseResponse ToCaseResponse(RegulatoryCase entity) =>
        new(entity.Id,
            entity.Title,
            entity.Description,
            entity.CaseTypeId,
            entity.Status.ToString(),
            entity.Priority.ToString(),
            entity.Country,
            entity.CreatedBy,
            entity.CreatedAt,
            entity.UpdatedAt);

    private static async Task<Results<Ok<CaseResponse>, NotFound, IResult>> TransitionCase(
        Guid id,
        AppDbContext db,
        Func<RegulatoryCase, ErrorOr<Success>> transition,
        CancellationToken cancellationToken)
    {
        var entity = await db.RegulatoryCases
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);

        if (entity is null)
        {
            return TypedResults.NotFound();
        }

        var result = transition(entity);
        if (result.IsError)
        {
            var error = result.Errors[0];
            return TypedResults.Problem(
                statusCode: StatusCodes.Status409Conflict,
                title: "Conflict",
                type: "https://datatracker.ietf.org/doc/html/rfc7231#section-6.5.8",
                detail: error.Description);
        }

        await db.SaveChangesAsync(cancellationToken);
        return TypedResults.Ok(ToCaseResponse(entity));
    }

    private static async Task<Results<Ok<IReadOnlyList<CaseStatusHistoryResponse>>, NotFound>> GetCaseHistory(
        Guid id,
        AppDbContext db,
        CancellationToken cancellationToken = default)
    {
        var exists = await db.RegulatoryCases
            .AsNoTracking()
            .AnyAsync(c => c.Id == id, cancellationToken);

        if (!exists)
        {
            return TypedResults.NotFound();
        }

        var history = await db.CaseStatusHistories
            .AsNoTracking()
            .Where(h => h.CaseId == id)
            .OrderBy(h => h.TransitionedAt)
            .Select(h => new CaseStatusHistoryResponse(
                h.Id,
                h.CaseId,
                h.FromStatus.ToString(),
                h.ToStatus.ToString(),
                h.TransitionedAt))
            .ToListAsync(cancellationToken);

        return TypedResults.Ok((IReadOnlyList<CaseStatusHistoryResponse>)history);
    }
}
