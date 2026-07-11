using System.Net;
using System.Net.Http.Json;
using HealthCasePlatform.Api.Cases;
using HealthCasePlatform.Api.Common;
using HealthCasePlatform.Domain.Enums;
using Shouldly;

namespace HealthCasePlatform.Api.Tests.Integration;

public sealed class AssignmentAuthorizationTests : IClassFixture<ApiFactory>
{
    private readonly ApiFactory _factory;

    public AssignmentAuthorizationTests(ApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task StartScientificReview_WhenCallerIsAssignedScientificReviewer_Returns200()
    {
        var privileged = _factory.CreateClient();
        var created = await privileged.CreateCaseAsync();
        await privileged.BringCaseToStateAsync(created.Id, CaseStatus.Submitted);
        await privileged.AssignScientificReviewerAsync(created.Id, "sci-1");

        var client = _factory.CreateClientAs("sci-1", AppRoles.ScientificReviewer);
        var response = await client.PostAsync($"/api/v1/cases/{created.Id}/scientific-review", content: null);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task StartScientificReview_WhenCallerIsUnassignedScientificReviewer_Returns403()
    {
        var privileged = _factory.CreateClient();
        var created = await privileged.CreateCaseAsync();
        await privileged.BringCaseToStateAsync(created.Id, CaseStatus.Submitted);

        var client = _factory.CreateClientAs("sci-1", AppRoles.ScientificReviewer);
        var response = await client.PostAsync($"/api/v1/cases/{created.Id}/scientific-review", content: null);

        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
        response.Content.Headers.ContentType?.MediaType.ShouldBe("application/problem+json");
    }

    [Fact]
    public async Task StartScientificReview_WhenCallerIsTeamLeaderOverride_Returns200()
    {
        var privileged = _factory.CreateClient();
        var created = await privileged.CreateCaseAsync();
        await privileged.BringCaseToStateAsync(created.Id, CaseStatus.Submitted);

        var client = _factory.CreateClientAs("chief", AppRoles.TeamLeader);
        var response = await client.PostAsync($"/api/v1/cases/{created.Id}/scientific-review", content: null);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task StartLegalReview_WhenCallerIsAssignedLegalReviewer_Returns200()
    {
        var privileged = _factory.CreateClient();
        var created = await privileged.CreateCaseAsync();
        await privileged.BringCaseToStateAsync(created.Id, CaseStatus.UnderScientificReview);
        await privileged.AssignLegalReviewerAsync(created.Id, "legal-1");

        var client = _factory.CreateClientAs("legal-1", AppRoles.LegalReviewer);
        var response = await client.PostAsync($"/api/v1/cases/{created.Id}/legal-review", content: null);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task StartLegalReview_WhenCallerIsUnassignedLegalReviewer_Returns403()
    {
        var privileged = _factory.CreateClient();
        var created = await privileged.CreateCaseAsync();
        await privileged.BringCaseToStateAsync(created.Id, CaseStatus.UnderScientificReview);

        var client = _factory.CreateClientAs("legal-1", AppRoles.LegalReviewer);
        var response = await client.PostAsync($"/api/v1/cases/{created.Id}/legal-review", content: null);

        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
        response.Content.Headers.ContentType?.MediaType.ShouldBe("application/problem+json");
    }

    [Fact]
    public async Task StartLegalReview_WhenCallerIsTeamLeaderOverride_Returns200()
    {
        var privileged = _factory.CreateClient();
        var created = await privileged.CreateCaseAsync();
        await privileged.BringCaseToStateAsync(created.Id, CaseStatus.UnderScientificReview);

        var client = _factory.CreateClientAs("chief", AppRoles.TeamLeader);
        var response = await client.PostAsync($"/api/v1/cases/{created.Id}/legal-review", content: null);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task AssignScientificReviewer_WithTeamLeaderRole_Returns200()
    {
        var privileged = _factory.CreateClient();
        var created = await privileged.CreateCaseAsync();

        var client = _factory.CreateClientAs("leader", AppRoles.TeamLeader);
        var response = await client.PostAsJsonAsync(
            $"/api/v1/cases/{created.Id}/assignment/scientific",
            new AssignReviewerRequest("sci-1"));

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task AssignScientificReviewer_WithWrongRole_Returns403()
    {
        var privileged = _factory.CreateClient();
        var created = await privileged.CreateCaseAsync();

        var client = _factory.CreateClientAs("sci-1", AppRoles.ScientificReviewer);
        var response = await client.PostAsJsonAsync(
            $"/api/v1/cases/{created.Id}/assignment/scientific",
            new AssignReviewerRequest("sci-1"));

        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
        response.Content.Headers.ContentType?.MediaType.ShouldBe("application/problem+json");
    }

    [Fact]
    public async Task AssignLegalReviewer_WithTeamLeaderRole_Returns200()
    {
        var privileged = _factory.CreateClient();
        var created = await privileged.CreateCaseAsync();

        var client = _factory.CreateClientAs("leader", AppRoles.TeamLeader);
        var response = await client.PostAsJsonAsync(
            $"/api/v1/cases/{created.Id}/assignment/legal",
            new AssignReviewerRequest("legal-1"));

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task AssignLegalReviewer_WithWrongRole_Returns403()
    {
        var privileged = _factory.CreateClient();
        var created = await privileged.CreateCaseAsync();

        var client = _factory.CreateClientAs("legal-1", AppRoles.LegalReviewer);
        var response = await client.PostAsJsonAsync(
            $"/api/v1/cases/{created.Id}/assignment/legal",
            new AssignReviewerRequest("legal-1"));

        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
        response.Content.Headers.ContentType?.MediaType.ShouldBe("application/problem+json");
    }
}
