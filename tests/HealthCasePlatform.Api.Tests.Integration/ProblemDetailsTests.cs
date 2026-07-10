using System.Net;
using System.Net.Http.Json;
using HealthCasePlatform.Api.Cases;
using HealthCasePlatform.Domain.Enums;
using Microsoft.AspNetCore.Mvc;
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

    [Fact]
    public async Task ValidationProblemDetails_InvalidPayload_Returns400WithErrorsDictionary()
    {
        var request = new CreateCaseRequest(
            Title: "",
            Description: null,
            CaseTypeId: Guid.NewGuid(),
            Priority: CasePriority.High,
            CreatedBy: "officer-1",
            Country: "PL");

        var response = await _client.PostAsJsonAsync("/api/v1/cases", request);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        response.Content.Headers.ContentType?.MediaType.ShouldBe("application/problem+json");

        var problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>();
        problem.ShouldNotBeNull();
        problem.Errors.ShouldNotBeEmpty();
        problem.Errors.ShouldContainKey(nameof(CreateCaseRequest.Title));
    }
}
