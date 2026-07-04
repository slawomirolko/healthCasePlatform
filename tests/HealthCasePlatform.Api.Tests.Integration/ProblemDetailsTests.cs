using System.Net;
using Shouldly;

namespace HealthCasePlatform.Api.Tests.Integration;

public sealed class ProblemDetailsTests : IClassFixture<ApiFactory>
{
    private readonly HttpClient _client;

    public ProblemDetailsTests(ApiFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task ProblemDetails_UnknownRoute_Returns404ProblemJson()
    {
        var response = await _client.GetAsync("/api/v1/does-not-exist");

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        response.Content.Headers.ContentType?.MediaType.ShouldBe("application/problem+json");
    }

    [Fact]
    public async Task ApiRoutePrefix_GetUnknownEndpoint_Returns404NotServerError()
    {
        var response = await _client.GetAsync("/api/v1/some-route");

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }
}
