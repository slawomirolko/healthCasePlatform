using ErrorOr;
using HealthCasePlatform.Api.Common;
using HealthCasePlatform.Application.Cases.Commands;
using HealthCasePlatform.Application.Cases.Models;
using HealthCasePlatform.Application.Cases.Queries;
using HealthCasePlatform.Domain.Cases;
using Mediator;
using Microsoft.AspNetCore.Http.HttpResults;

namespace HealthCasePlatform.Api.Cases;

public static class CasesEndpoints
{
    public static RouteGroupBuilder MapCasesEndpoints(this RouteGroupBuilder group)
    {
        group.MapPost("/cases", CreateCase)
            .WithName("CreateCase")
            .AddEndpointFilter<ValidationFilter<CreateCaseRequest>>()
            .RequireAuthorization(b => b.RequireRole(AppRoles.CaseOfficer));

        group.MapGet("/cases/{id:guid}", GetCase)
            .WithName("GetCase")
            .RequireAuthorization();

        group.MapGet("/cases", ListCases)
            .WithName("ListCases")
            .RequireAuthorization();

        group.MapPost("/cases/{id:guid}/submission", SubmitCase)
            .WithName("SubmitCase")
            .RequireAuthorization(b => b.RequireRole(AppRoles.CaseOfficer));

        group.MapPost("/cases/{id:guid}/scientific-review", StartScientificReview)
            .WithName("StartScientificReview")
            .RequireAuthorization(b => b.RequireRole(AppRoles.ScientificReviewer));

        group.MapPost("/cases/{id:guid}/legal-review", StartLegalReview)
            .WithName("StartLegalReview")
            .RequireAuthorization(b => b.RequireRole(AppRoles.LegalReviewer));

        group.MapPost("/cases/{id:guid}/decision-request", RequestDecision)
            .WithName("RequestDecision")
            .RequireAuthorization(b => b.RequireRole(AppRoles.TeamLeader));

        group.MapPost("/cases/{id:guid}/approval", ApproveCase)
            .WithName("ApproveCase")
            .RequireAuthorization(b => b.RequireRole(AppRoles.TeamLeader));

        group.MapPost("/cases/{id:guid}/rejection", RejectCase)
            .WithName("RejectCase")
            .RequireAuthorization(b => b.RequireRole(AppRoles.TeamLeader));

        group.MapGet("/cases/{id:guid}/history", GetCaseHistory)
            .WithName("GetCaseHistory")
            .RequireAuthorization();

        return group;
    }

    private static async Task<Ok<PagedResponse<CaseListItemResponse>>> ListCases(
        [AsParameters] ListCasesRequest request,
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        var query = new ListCasesQuery(request.Status, request.Priority, request.Country, request.Page, request.PageSize);
        var result = await mediator.Send(query, cancellationToken);

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
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        var entity = await mediator.Send(new GetCaseQuery(id), cancellationToken);

        if (entity is null)
        {
            return TypedResults.NotFound();
        }

        return TypedResults.Ok(ToCaseResponse(entity));
    }

    private static async Task<Results<Created<CreateCaseResponse>, ValidationProblem>> CreateCase(
        CreateCaseRequest request,
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        var command = new CreateCaseCommand(
            request.Title,
            request.Description ?? string.Empty,
            request.CaseTypeId,
            request.Priority,
            request.CreatedBy,
            request.Country);

        var result = await mediator.Send(command, cancellationToken);

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
        IMediator mediator,
        CancellationToken cancellationToken = default)
    {
        var result = await mediator.Send(new SubmitCaseCommand(id), cancellationToken);
        return MapTransition(result);
    }

    private static async Task<Results<Ok<CaseResponse>, NotFound, IResult>> StartScientificReview(
        Guid id,
        IMediator mediator,
        CancellationToken cancellationToken = default)
    {
        var result = await mediator.Send(new StartScientificReviewCommand(id), cancellationToken);
        return MapTransition(result);
    }

    private static async Task<Results<Ok<CaseResponse>, NotFound, IResult>> StartLegalReview(
        Guid id,
        IMediator mediator,
        CancellationToken cancellationToken = default)
    {
        var result = await mediator.Send(new StartLegalReviewCommand(id), cancellationToken);
        return MapTransition(result);
    }

    private static async Task<Results<Ok<CaseResponse>, NotFound, IResult>> RequestDecision(
        Guid id,
        IMediator mediator,
        CancellationToken cancellationToken = default)
    {
        var result = await mediator.Send(new RequestDecisionCommand(id), cancellationToken);
        return MapTransition(result);
    }

    private static async Task<Results<Ok<CaseResponse>, NotFound, IResult>> ApproveCase(
        Guid id,
        IMediator mediator,
        CancellationToken cancellationToken = default)
    {
        var result = await mediator.Send(new ApproveCaseCommand(id), cancellationToken);
        return MapTransition(result);
    }

    private static async Task<Results<Ok<CaseResponse>, NotFound, IResult>> RejectCase(
        Guid id,
        IMediator mediator,
        CancellationToken cancellationToken = default)
    {
        var result = await mediator.Send(new RejectCaseCommand(id), cancellationToken);
        return MapTransition(result);
    }

    private static async Task<Results<Ok<IReadOnlyList<CaseStatusHistoryResponse>>, NotFound>> GetCaseHistory(
        Guid id,
        IMediator mediator,
        CancellationToken cancellationToken = default)
    {
        var result = await mediator.Send(new GetCaseHistoryQuery(id), cancellationToken);

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
