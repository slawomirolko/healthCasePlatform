using System.Net;
using System.Net.Http.Json;
using HealthCasePlatform.Api.Cases;
using HealthCasePlatform.Api.Common;
using HealthCasePlatform.Domain.Enums;
using Shouldly;

namespace HealthCasePlatform.Api.Tests.Integration;

public sealed class WorkflowAuthorizationTests : IClassFixture<ApiFactory>
{
    private readonly ApiFactory _factory;

    public WorkflowAuthorizationTests(ApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task CreateCase_WithCaseOfficerRole_Returns201()
    {
        var client = _factory.CreateClientWithRoles(AppRoles.CaseOfficer);

        var response = await client.PostAsJsonAsync("/api/v1/cases", CaseTestSeeder.ValidRequest());

        response.StatusCode.ShouldBe(HttpStatusCode.Created);
    }

    [Fact]
    public async Task CreateCase_WithWrongRole_Returns403()
    {
        var client = _factory.CreateClientWithRoles(AppRoles.Auditor);

        var response = await client.PostAsJsonAsync("/api/v1/cases", CaseTestSeeder.ValidRequest());

        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
        response.Content.Headers.ContentType?.MediaType.ShouldBe("application/problem+json");
    }

    [Fact]
    public async Task SubmitCase_WithCaseOfficerRole_Returns200()
    {
        var privileged = _factory.CreateClient();
        var created = await privileged.CreateCaseAsync();

        var client = _factory.CreateClientWithRoles(AppRoles.CaseOfficer);
        var response = await client.PostAsync($"/api/v1/cases/{created.Id}/submission", content: null);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task SubmitCase_WithWrongRole_Returns403()
    {
        var privileged = _factory.CreateClient();
        var created = await privileged.CreateCaseAsync();

        var client = _factory.CreateClientWithRoles(AppRoles.Auditor);
        var response = await client.PostAsync($"/api/v1/cases/{created.Id}/submission", content: null);

        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
        response.Content.Headers.ContentType?.MediaType.ShouldBe("application/problem+json");
    }

    [Fact]
    public async Task StartScientificReview_WithScientificReviewerRole_Returns200()
    {
        var privileged = _factory.CreateClient();
        var created = await privileged.CreateCaseAsync();
        await privileged.BringCaseToStateAsync(created.Id, CaseStatus.Submitted);

        var client = _factory.CreateClientWithRoles(AppRoles.ScientificReviewer);
        var response = await client.PostAsync($"/api/v1/cases/{created.Id}/scientific-review", content: null);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task StartLegalReview_WithLegalReviewerRole_Returns200()
    {
        var privileged = _factory.CreateClient();
        var created = await privileged.CreateCaseAsync();
        await privileged.BringCaseToStateAsync(created.Id, CaseStatus.UnderScientificReview);

        var client = _factory.CreateClientWithRoles(AppRoles.LegalReviewer);
        var response = await client.PostAsync($"/api/v1/cases/{created.Id}/legal-review", content: null);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task StartLegalReview_WithWrongRole_Returns403()
    {
        var privileged = _factory.CreateClient();
        var created = await privileged.CreateCaseAsync();
        await privileged.BringCaseToStateAsync(created.Id, CaseStatus.UnderScientificReview);

        var client = _factory.CreateClientWithRoles(AppRoles.CaseOfficer);
        var response = await client.PostAsync($"/api/v1/cases/{created.Id}/legal-review", content: null);

        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
        response.Content.Headers.ContentType?.MediaType.ShouldBe("application/problem+json");
    }

    [Fact]
    public async Task RequestDecision_WithTeamLeaderRole_Returns200()
    {
        var privileged = _factory.CreateClient();
        var created = await privileged.CreateCaseAsync();
        await privileged.BringCaseToStateAsync(created.Id, CaseStatus.UnderLegalReview);

        var client = _factory.CreateClientWithRoles(AppRoles.TeamLeader);
        var response = await client.PostAsync($"/api/v1/cases/{created.Id}/decision-request", content: null);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task RequestDecision_WithWrongRole_Returns403()
    {
        var privileged = _factory.CreateClient();
        var created = await privileged.CreateCaseAsync();
        await privileged.BringCaseToStateAsync(created.Id, CaseStatus.UnderLegalReview);

        var client = _factory.CreateClientWithRoles(AppRoles.ScientificReviewer);
        var response = await client.PostAsync($"/api/v1/cases/{created.Id}/decision-request", content: null);

        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
        response.Content.Headers.ContentType?.MediaType.ShouldBe("application/problem+json");
    }

    [Fact]
    public async Task ApproveCase_WithTeamLeaderRole_Returns200()
    {
        var privileged = _factory.CreateClient();
        var created = await privileged.CreateCaseAsync();
        await privileged.BringCaseToStateAsync(created.Id, CaseStatus.PendingDecision);

        var client = _factory.CreateClientWithRoles(AppRoles.TeamLeader);
        var response = await client.PostAsync($"/api/v1/cases/{created.Id}/approval", content: null);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task ApproveCase_WithWrongRole_Returns403()
    {
        var privileged = _factory.CreateClient();
        var created = await privileged.CreateCaseAsync();
        await privileged.BringCaseToStateAsync(created.Id, CaseStatus.PendingDecision);

        var client = _factory.CreateClientWithRoles(AppRoles.Auditor);
        var response = await client.PostAsync($"/api/v1/cases/{created.Id}/approval", content: null);

        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
        response.Content.Headers.ContentType?.MediaType.ShouldBe("application/problem+json");
    }

    [Fact]
    public async Task ListCases_WithAuditorRole_Returns200()
    {
        var client = _factory.CreateClientWithRoles(AppRoles.Auditor);

        var response = await client.GetAsync("/api/v1/cases?page=1&pageSize=10");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        response.Content.Headers.ContentType?.MediaType.ShouldBe("application/json");
    }
}
