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

    [Fact]
    public async Task GetCase_WhenCaseExists_ReturnsCase()
    {
        var request = ValidRequest();
        var createResponse = await _client.PostAsJsonAsync("/api/v1/cases", request);
        createResponse.StatusCode.ShouldBe(HttpStatusCode.Created);

        var created = await createResponse.Content.ReadFromJsonAsync<CreateCaseResponse>();
        created.ShouldNotBeNull();

        var getResponse = await _client.GetAsync($"/api/v1/cases/{created.Id}");

        getResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
        getResponse.Content.Headers.ContentType?.MediaType.ShouldBe("application/json");

        var body = await getResponse.Content.ReadFromJsonAsync<CaseResponse>();
        body.ShouldNotBeNull();
        body.Id.ShouldBe(created.Id);
        body.Title.ShouldBe(request.Title);
        body.Description.ShouldBe(request.Description ?? string.Empty);
        body.CaseTypeId.ShouldBe(request.CaseTypeId);
        body.Priority.ShouldBe(request.Priority.ToString());
        body.CreatedBy.ShouldBe(request.CreatedBy);
        body.Status.ShouldBe("Draft");
        body.CreatedAt.ShouldBeGreaterThan(DateTime.MinValue);
        body.UpdatedAt.ShouldBeNull();
    }

    [Fact]
    public async Task GetCase_WhenCaseUnknown_Returns404()
    {
        var response = await _client.GetAsync($"/api/v1/cases/{Guid.NewGuid()}");

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        response.Content.Headers.ContentType?.MediaType.ShouldBe("application/problem+json");
    }
}
