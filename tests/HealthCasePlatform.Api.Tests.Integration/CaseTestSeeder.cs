using System.Net;
using System.Net.Http.Json;
using HealthCasePlatform.Api.Cases;
using HealthCasePlatform.Domain.Enums;
using Shouldly;

namespace HealthCasePlatform.Api.Tests.Integration;

internal static class CaseTestSeeder
{
    public static CreateCaseRequest ValidRequest() => new(
        "Food safety incident #42",
        "Initial report",
        Guid.NewGuid(),
        CasePriority.High,
        "officer-1",
        "PL");

    public static async Task<CreateCaseResponse> CreateCaseAsync(this HttpClient client) =>
        await client.CreateCaseAsync(ValidRequest());

    public static async Task<CreateCaseResponse> CreateCaseAsync(this HttpClient client, CreateCaseRequest request)
    {
        var createResponse = await client.PostAsJsonAsync("/api/v1/cases", request);
        createResponse.StatusCode.ShouldBe(HttpStatusCode.Created);
        return (await createResponse.Content.ReadFromJsonAsync<CreateCaseResponse>())!;
    }

    public static async Task BringCaseToStateAsync(this HttpClient client, Guid caseId, CaseStatus target)
    {
        await client.PostAsync($"/api/v1/cases/{caseId}/submission", content: null);
        if (target == CaseStatus.Submitted)
        {
            return;
        }

        await client.PostAsync($"/api/v1/cases/{caseId}/scientific-review", content: null);
        if (target == CaseStatus.UnderScientificReview)
        {
            return;
        }

        await client.PostAsync($"/api/v1/cases/{caseId}/legal-review", content: null);
        if (target == CaseStatus.UnderLegalReview)
        {
            return;
        }

        await client.PostAsync($"/api/v1/cases/{caseId}/decision-request", content: null);
        if (target == CaseStatus.PendingDecision)
        {
            return;
        }

        if (target == CaseStatus.Approved)
        {
            await client.PostAsync($"/api/v1/cases/{caseId}/approval", content: null);
            return;
        }

        if (target == CaseStatus.Rejected)
        {
            await client.PostAsync($"/api/v1/cases/{caseId}/rejection", content: null);
            return;
        }

        throw new ArgumentOutOfRangeException(nameof(target), $"Unsupported target status for test arrange: {target}");
    }
}
