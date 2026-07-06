using System.Net;
using System.Net.Http.Json;
using HealthCasePlatform.Api.Cases;
using HealthCasePlatform.Domain.Cases;
using HealthCasePlatform.Domain.Enums;
using HealthCasePlatform.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;

namespace HealthCasePlatform.Api.Tests.Integration;

public sealed class ListCasesTests : IClassFixture<ApiFactory>
{
    private readonly HttpClient _client;
    private readonly ApiFactory _factory;

    public ListCasesTests(ApiFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    private static RegulatoryCase NewCase(string title, CasePriority priority, string country, bool submit = false)
    {
        var entity = RegulatoryCase.Create(title, "desc", Guid.NewGuid(), priority, "officer-1", country).Value;
        if (submit)
        {
            entity.Submit();
        }

        return entity;
    }

    private async Task SeedAsync(params RegulatoryCase[] cases)
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await db.RegulatoryCases.ExecuteDeleteAsync(CancellationToken.None);
        await db.RegulatoryCases.AddRangeAsync(cases);
        await db.SaveChangesAsync(CancellationToken.None);
    }

    private async Task<PagedResponse<CaseListItemResponse>> GetListAsync(string query)
    {
        var response = await _client.GetAsync($"/api/v1/cases{query}");
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        response.Content.Headers.ContentType?.MediaType.ShouldBe("application/json");
        return (await response.Content.ReadFromJsonAsync<PagedResponse<CaseListItemResponse>>())!;
    }

    [Fact]
    public async Task ListCases_WithNoFilters_ReturnsFirstPageWithPaginationMetadata()
    {
        await SeedAsync(
            NewCase("c1", CasePriority.Low, "PL"),
            NewCase("c2", CasePriority.Low, "PL"),
            NewCase("c3", CasePriority.Low, "PL"));

        var body = await GetListAsync("?page=1&pageSize=2");

        body.Page.ShouldBe(1);
        body.PageSize.ShouldBe(2);
        body.Items.Count.ShouldBe(2);
        body.TotalCount.ShouldBe(3);
        body.TotalPages.ShouldBe(2);
    }

    [Fact]
    public async Task ListCases_WhenPageSizeExceeds100_ClampsTo100()
    {
        await SeedAsync(NewCase("only", CasePriority.Low, "PL"));

        var body = await GetListAsync("?pageSize=999");

        body.PageSize.ShouldBe(100);
        body.TotalCount.ShouldBe(1);
    }

    [Fact]
    public async Task ListCases_WhenPageParamIsZero_ClampsToOne()
    {
        await SeedAsync(NewCase("only", CasePriority.Low, "PL"));

        var body = await GetListAsync("?page=0");

        body.Page.ShouldBe(1);
        body.TotalCount.ShouldBe(1);
    }

    [Fact]
    public async Task ListCases_FiltersByStatus_ReturnsOnlyMatchingCases()
    {
        await SeedAsync(
            NewCase("draft", CasePriority.Low, "PL", submit: false),
            NewCase("submitted", CasePriority.Low, "PL", submit: true));

        var body = await GetListAsync("?status=Submitted");

        body.TotalCount.ShouldBe(1);
        body.Items.Single().Title.ShouldBe("submitted");
    }

    [Fact]
    public async Task ListCases_FiltersByPriority_ReturnsOnlyMatchingCases()
    {
        await SeedAsync(
            NewCase("low", CasePriority.Low, "PL"),
            NewCase("high", CasePriority.High, "PL"));

        var body = await GetListAsync("?priority=High");

        body.TotalCount.ShouldBe(1);
        body.Items.Single().Title.ShouldBe("high");
    }

    [Fact]
    public async Task ListCases_FiltersByCountry_ReturnsOnlyMatchingCases()
    {
        await SeedAsync(
            NewCase("polish", CasePriority.Low, "PL"),
            NewCase("german", CasePriority.Low, "DE"));

        var bodyUpper = await GetListAsync("?country=PL");
        bodyUpper.TotalCount.ShouldBe(1);
        bodyUpper.Items.Single().Title.ShouldBe("polish");

        var bodyLower = await GetListAsync("?country=pl");
        bodyLower.TotalCount.ShouldBe(1);
        bodyLower.Items.Single().Title.ShouldBe("polish");
    }

    [Fact]
    public async Task ListCases_CombinesStatusPriorityAndCountryFilters()
    {
        await SeedAsync(
            NewCase("match", CasePriority.High, "PL", submit: true),
            NewCase("wrongStatus", CasePriority.High, "PL", submit: false),
            NewCase("wrongPriority", CasePriority.Low, "PL", submit: true),
            NewCase("wrongCountry", CasePriority.High, "DE", submit: true));

        var body = await GetListAsync("?status=Submitted&priority=High&country=PL");

        body.TotalCount.ShouldBe(1);
        body.Items.Single().Title.ShouldBe("match");
    }

    [Fact]
    public async Task ListCases_WhenNoMatches_ReturnsEmptyItemsAndZeroTotal()
    {
        await SeedAsync(NewCase("polish", CasePriority.Low, "PL"));

        var body = await GetListAsync("?country=DE");

        body.TotalCount.ShouldBe(0);
        body.TotalPages.ShouldBe(0);
        body.Items.ShouldBeEmpty();
    }

    [Fact]
    public async Task ListCases_ReturnsItemsOrderedByCreatedAtDescendingThenIdDescending()
    {
        var first = NewCase("first", CasePriority.Low, "PL");
        var second = NewCase("second", CasePriority.Low, "PL");

        await SeedAsync(first, second);

        var body = await GetListAsync("?pageSize=10");

        body.Items.First().Title.ShouldBe("second");
        body.Items.Last().Title.ShouldBe("first");
    }
}
