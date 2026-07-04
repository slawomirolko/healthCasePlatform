using System.Net;
using Shouldly;

namespace HealthCasePlatform.Api.Tests.Integration;

public sealed class HealthEndpointTests : IClassFixture<ApiFactory>
{
    private readonly HttpClient _client;

    public HealthEndpointTests(ApiFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Health_WhenAppRunning_Returns200AndHealthy()
    {
        var response = await _client.GetAsync("/health");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var body = await response.Content.ReadAsStringAsync();
        body.ShouldBe("Healthy");
    }
}
