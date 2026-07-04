using System.Net;
using Shouldly;

namespace HealthCasePlatform.Api.Tests.Integration;

public sealed class SwaggerEndpointTests : IClassFixture<ApiFactory>
{
    private readonly HttpClient _client;

    public SwaggerEndpointTests(ApiFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Swagger_WhenRequested_Returns200()
    {
        var response = await _client.GetAsync("/swagger/index.html");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }
}
