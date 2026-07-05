using System.Net;
using System.Net.Http.Json;
using HealthCasePlatform.Api.Cases;
using HealthCasePlatform.Domain.Enums;
using Shouldly;

namespace HealthCasePlatform.Api.Tests.Integration;

public sealed class CasesEndpointsTests : IClassFixture<ApiFactory>
{
    private readonly HttpClient _client;

    public CasesEndpointsTests(ApiFactory factory)
    {
        _client = factory.CreateClient();
    }

    private static CreateCaseRequest ValidRequest() => new(
        "Food safety incident #42",
        "Initial report",
        Guid.NewGuid(),
        CasePriority.High,
        "officer-1");

    [Fact]
    public async Task CreateCase_WithValidPayload_Returns201AndCreatedCase()
    {
        var request = ValidRequest();

        var response = await _client.PostAsJsonAsync("/api/v1/cases", request);

        response.StatusCode.ShouldBe(HttpStatusCode.Created);

        var location = response.Headers.Location?.ToString();
        location.ShouldStartWith("/api/v1/cases/");

        var body = await response.Content.ReadFromJsonAsync<CreateCaseResponse>();
        body.ShouldNotBeNull();
        body.Id.ShouldNotBe(Guid.Empty);
        body.Title.ShouldBe(request.Title);
        body.Description.ShouldBe(request.Description);
        body.CaseTypeId.ShouldBe(request.CaseTypeId);
        body.Priority.ShouldBe(request.Priority.ToString());
        body.CreatedBy.ShouldBe(request.CreatedBy);
        body.Status.ShouldBe("Draft");
        body.CreatedAt.ShouldBeGreaterThan(DateTime.MinValue);

        response.Content.Headers.ContentType?.MediaType.ShouldBe("application/json");
    }

    [Fact]
    public async Task CreateCase_WithEmptyTitle_Returns400()
    {
        var request = ValidRequest() with { Title = "" };

        var response = await _client.PostAsJsonAsync("/api/v1/cases", request);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        response.Content.Headers.ContentType?.MediaType.ShouldBe("application/problem+json");
    }

    [Fact]
    public async Task CreateCase_WithMissingTitleField_Returns400()
    {
        var payload = new
        {
            description = "no title here",
            caseTypeId = Guid.NewGuid(),
            priority = CasePriority.Medium,
            createdBy = "officer-1"
        };

        var response = await _client.PostAsJsonAsync("/api/v1/cases", payload);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        response.Content.Headers.ContentType?.MediaType.ShouldBe("application/problem+json");
    }
}
