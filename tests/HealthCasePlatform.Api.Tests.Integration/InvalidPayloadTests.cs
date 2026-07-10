using System.Net;
using System.Net.Http.Json;
using System.Text;
using HealthCasePlatform.Api.Cases;
using HealthCasePlatform.Domain.Enums;
using Shouldly;

namespace HealthCasePlatform.Api.Tests.Integration;

public sealed class InvalidPayloadTests : IClassFixture<ApiFactory>
{
    private readonly HttpClient _client;

    public InvalidPayloadTests(ApiFactory factory)
    {
        _client = factory.CreateClient();
    }

    private static CreateCaseRequest ValidRequest() => new(
        "Food safety incident #42",
        "Initial report",
        Guid.NewGuid(),
        CasePriority.High,
        "officer-1",
        "PL");

    [Fact]
    public async Task CreateCase_WithMalformedJson_Returns400ProblemDetails()
    {
        using var content = new StringContent("{ invalid", Encoding.UTF8, "application/json");

        var response = await _client.PostAsync("/api/v1/cases", content);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        response.Content.Headers.ContentType?.MediaType.ShouldBe("application/problem+json");
    }

    [Fact]
    public async Task CreateCase_WithTitleTooLong_Returns400ProblemDetails()
    {
        var request = ValidRequest() with { Title = new string('x', 201) };

        var response = await _client.PostAsJsonAsync("/api/v1/cases", request);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        response.Content.Headers.ContentType?.MediaType.ShouldBe("application/problem+json");
    }

    [Fact]
    public async Task CreateCase_WithDescriptionTooLong_Returns400ProblemDetails()
    {
        var request = ValidRequest() with { Description = new string('x', 2001) };

        var response = await _client.PostAsJsonAsync("/api/v1/cases", request);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        response.Content.Headers.ContentType?.MediaType.ShouldBe("application/problem+json");
    }

    [Fact]
    public async Task CreateCase_WithCreatedByTooLong_Returns400ProblemDetails()
    {
        var request = ValidRequest() with { CreatedBy = new string('x', 101) };

        var response = await _client.PostAsJsonAsync("/api/v1/cases", request);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        response.Content.Headers.ContentType?.MediaType.ShouldBe("application/problem+json");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(999)]
    public async Task CreateCase_WithPriorityOutOfRange_Returns400ProblemDetails(int priority)
    {
        var payload = new
        {
            title = "Food safety incident #42",
            description = "Initial report",
            caseTypeId = Guid.NewGuid(),
            priority,
            createdBy = "officer-1",
            country = "PL"
        };

        var response = await _client.PostAsJsonAsync("/api/v1/cases", payload);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        response.Content.Headers.ContentType?.MediaType.ShouldBe("application/problem+json");
    }
}
