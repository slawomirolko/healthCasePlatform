using ErrorOr;
using HealthCasePlatform.Api.Common;
using HealthCasePlatform.Application.Cases;
using HealthCasePlatform.Application.Cases.Models;
using HealthCasePlatform.Domain.Cases;
using Microsoft.AspNetCore.Http.HttpResults;

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
        ICaseService caseService,
        CancellationToken cancellationToken)
    {
        var filter = new CaseListFilter(request.Status, request.Priority, request.Country);
        var result = await caseService.ListAsync(filter, request.Page, request.PageSize, cancellationToken);

        var items = result.Items
            .Select(c => new CaseListItemResponse(
                c.Id,
                c.Title,
                c.Country,
                c.Status.ToString(),
                c.Priority.ToString(),
                c.CreatedAt))
            .ToList();

        return TypedResults.Ok(new PagedResponse<CaseListItemResponse>(
            items,
            result.Page,
            result.PageSize,
            result.TotalCount,
            result.TotalPages));
    }

    private static async Task<Results<Ok<CaseResponse>, NotFound>> GetCase(
        Guid id,
        ICaseService caseService,
        CancellationToken cancellationToken)
    {
        var entity = await caseService.GetByIdAsync(id, cancellationToken);

        if (entity is null)
        {
            return TypedResults.NotFound();
        }

        return TypedResults.Ok(ToCaseResponse(entity));
    }

    private static async Task<Results<Created<CreateCaseResponse>, ValidationProblem>> CreateCase(
        CreateCaseRequest request,
        ICaseService caseService,
        CancellationToken cancellationToken)
    {
        var result = await caseService.CreateAsync(
            request.Title,
            request.Description ?? string.Empty,
            request.CaseTypeId,
            request.Priority,
            request.CreatedBy,
            request.Country,
            cancellationToken);

        if (result.IsError)
        {
            var errors = result.Errors.ToDictionary(e => e.Code, e => new[] { e.Description });
            return TypedResults.ValidationProblem(errors);
        }

        var entity = result.Value;

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

    private static async Task<Results<Ok<CaseResponse>, NotFound, IResult>> SubmitCase(
        Guid id,
        ICaseService caseService,
        CancellationToken cancellationToken = default)
    {
        var result = await caseService.SubmitAsync(id, cancellationToken);
        return MapTransition(result);
    }

    private static async Task<Results<Ok<CaseResponse>, NotFound, IResult>> StartScientificReview(
        Guid id,
        ICaseService caseService,
        CancellationToken cancellationToken = default)
    {
        var result = await caseService.StartScientificReviewAsync(id, cancellationToken);
        return MapTransition(result);
    }

    private static async Task<Results<Ok<CaseResponse>, NotFound, IResult>> StartLegalReview(
        Guid id,
        ICaseService caseService,
        CancellationToken cancellationToken = default)
    {
        var result = await caseService.StartLegalReviewAsync(id, cancellationToken);
        return MapTransition(result);
    }

    private static async Task<Results<Ok<CaseResponse>, NotFound, IResult>> RequestDecision(
        Guid id,
        ICaseService caseService,
        CancellationToken cancellationToken = default)
    {
        var result = await caseService.RequestDecisionAsync(id, cancellationToken);
        return MapTransition(result);
    }

    private static async Task<Results<Ok<CaseResponse>, NotFound, IResult>> ApproveCase(
        Guid id,
        ICaseService caseService,
        CancellationToken cancellationToken = default)
    {
        var result = await caseService.ApproveAsync(id, cancellationToken);
        return MapTransition(result);
    }

    private static async Task<Results<Ok<CaseResponse>, NotFound, IResult>> RejectCase(
        Guid id,
        ICaseService caseService,
        CancellationToken cancellationToken = default)
    {
        var result = await caseService.RejectAsync(id, cancellationToken);
        return MapTransition(result);
    }

    private static async Task<Results<Ok<IReadOnlyList<CaseStatusHistoryResponse>>, NotFound>> GetCaseHistory(
        Guid id,
        ICaseService caseService,
        CancellationToken cancellationToken = default)
    {
        var result = await caseService.GetHistoryAsync(id, cancellationToken);

        if (result.IsError)
        {
            return TypedResults.NotFound();
        }

        var history = result.Value
            .Select(h => new CaseStatusHistoryResponse(
                h.Id,
                h.CaseId,
                h.FromStatus.ToString(),
                h.ToStatus.ToString(),
                h.TransitionedAt))
            .ToList();

        return TypedResults.Ok((IReadOnlyList<CaseStatusHistoryResponse>)history);
    }

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

    private static Results<Ok<CaseResponse>, NotFound, IResult> MapTransition(ErrorOr<RegulatoryCase> result)
    {
        if (result.IsError)
        {
            var error = result.Errors[0];
            if (error.Type == ErrorType.NotFound)
            {
                return TypedResults.NotFound();
            }

            return TypedResults.Problem(
                statusCode: StatusCodes.Status409Conflict,
                title: "Conflict",
                type: "https://datatracker.ietf.org/doc/html/rfc7231#section-6.5.8",
                detail: error.Description);
        }

        return TypedResults.Ok(ToCaseResponse(result.Value));
    }
}
