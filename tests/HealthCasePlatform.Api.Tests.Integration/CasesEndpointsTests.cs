using System.Net;
using System.Net.Http.Json;
using HealthCasePlatform.Api.Cases;
using HealthCasePlatform.Domain.Enums;
using Microsoft.AspNetCore.Mvc;
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
        "officer-1",
        "PL");

    private async Task<CreateCaseResponse> CreateCaseAsync()
    {
        var createResponse = await _client.PostAsJsonAsync("/api/v1/cases", ValidRequest());
        createResponse.StatusCode.ShouldBe(HttpStatusCode.Created);
        return (await createResponse.Content.ReadFromJsonAsync<CreateCaseResponse>())!;
    }

    private async Task BringCaseToStateAsync(Guid caseId, CaseStatus target)
    {
        await _client.PostAsync($"/api/v1/cases/{caseId}/submission", content: null);
        if (target == CaseStatus.Submitted)
        {
            return;
        }

        await _client.PostAsync($"/api/v1/cases/{caseId}/scientific-review", content: null);
        if (target == CaseStatus.UnderScientificReview)
        {
            return;
        }

        await _client.PostAsync($"/api/v1/cases/{caseId}/legal-review", content: null);
        if (target == CaseStatus.UnderLegalReview)
        {
            return;
        }

        await _client.PostAsync($"/api/v1/cases/{caseId}/decision-request", content: null);
        if (target == CaseStatus.PendingDecision)
        {
            return;
        }

        if (target == CaseStatus.Approved)
        {
            await _client.PostAsync($"/api/v1/cases/{caseId}/approval", content: null);
            return;
        }

        if (target == CaseStatus.Rejected)
        {
            await _client.PostAsync($"/api/v1/cases/{caseId}/rejection", content: null);
            return;
        }

        throw new ArgumentOutOfRangeException(nameof(target), $"Unsupported target status for test arrange: {target}");
    }

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
        body.Country.ShouldBe("PL");
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
            createdBy = "officer-1",
            country = "PL"
        };

        var response = await _client.PostAsJsonAsync("/api/v1/cases", payload);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        response.Content.Headers.ContentType?.MediaType.ShouldBe("application/problem+json");
    }

    [Theory]
    [InlineData("POL")]
    [InlineData("P")]
    [InlineData("12")]
    public async Task CreateCase_WithInvalidCountryFormat_Returns400(string country)
    {
        var request = ValidRequest() with { Country = country };

        var response = await _client.PostAsJsonAsync("/api/v1/cases", request);

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
        body.Country.ShouldBe("PL");
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

    [Fact]
    public async Task SubmitCase_WhenCaseIsDraft_ReturnsSubmittedCase()
    {
        var request = ValidRequest();
        var createResponse = await _client.PostAsJsonAsync("/api/v1/cases", request);
        createResponse.StatusCode.ShouldBe(HttpStatusCode.Created);

        var created = await createResponse.Content.ReadFromJsonAsync<CreateCaseResponse>();
        created.ShouldNotBeNull();

        var submitResponse = await _client.PostAsync($"/api/v1/cases/{created.Id}/submission", content: null);

        submitResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
        submitResponse.Content.Headers.ContentType?.MediaType.ShouldBe("application/json");

        var body = await submitResponse.Content.ReadFromJsonAsync<CaseResponse>();
        body.ShouldNotBeNull();
        body.Id.ShouldBe(created.Id);
        body.Status.ShouldBe("Submitted");
        body.UpdatedAt.ShouldNotBeNull();

        var getResponse = await _client.GetAsync($"/api/v1/cases/{created.Id}");
        getResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
        var persisted = await getResponse.Content.ReadFromJsonAsync<CaseResponse>();
        persisted.ShouldNotBeNull();
        persisted.Status.ShouldBe("Submitted");
        persisted.UpdatedAt.ShouldNotBeNull();
    }

    [Fact]
    public async Task SubmitCase_WhenCaseUnknown_Returns404()
    {
        var response = await _client.PostAsync($"/api/v1/cases/{Guid.NewGuid()}/submission", content: null);

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        response.Content.Headers.ContentType?.MediaType.ShouldBe("application/problem+json");
    }

    [Fact]
    public async Task SubmitCase_WhenCaseAlreadySubmitted_Returns409()
    {
        var request = ValidRequest();
        var createResponse = await _client.PostAsJsonAsync("/api/v1/cases", request);
        createResponse.StatusCode.ShouldBe(HttpStatusCode.Created);

        var created = await createResponse.Content.ReadFromJsonAsync<CreateCaseResponse>();
        created.ShouldNotBeNull();

        var firstSubmit = await _client.PostAsync($"/api/v1/cases/{created.Id}/submission", content: null);
        firstSubmit.StatusCode.ShouldBe(HttpStatusCode.OK);

        var secondSubmit = await _client.PostAsync($"/api/v1/cases/{created.Id}/submission", content: null);

        secondSubmit.StatusCode.ShouldBe(HttpStatusCode.Conflict);
        secondSubmit.Content.Headers.ContentType?.MediaType.ShouldBe("application/problem+json");

        var body = await secondSubmit.Content.ReadFromJsonAsync<ProblemDetails>();
        body.ShouldNotBeNull();
        body.Detail.ShouldBe("Only a draft case can be submitted.");
    }

    [Fact]
    public async Task StartScientificReview_WhenCaseIsSubmitted_ReturnsUnderScientificReviewCase()
    {
        var request = ValidRequest();
        var createResponse = await _client.PostAsJsonAsync("/api/v1/cases", request);
        createResponse.StatusCode.ShouldBe(HttpStatusCode.Created);
        var created = await createResponse.Content.ReadFromJsonAsync<CreateCaseResponse>();
        created.ShouldNotBeNull();

        var submitResponse = await _client.PostAsync($"/api/v1/cases/{created.Id}/submission", content: null);
        submitResponse.StatusCode.ShouldBe(HttpStatusCode.OK);

        var response = await _client.PostAsync($"/api/v1/cases/{created.Id}/scientific-review", content: null);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        response.Content.Headers.ContentType?.MediaType.ShouldBe("application/json");

        var body = await response.Content.ReadFromJsonAsync<CaseResponse>();
        body.ShouldNotBeNull();
        body.Id.ShouldBe(created.Id);
        body.Status.ShouldBe("UnderScientificReview");
        body.UpdatedAt.ShouldNotBeNull();

        var getResponse = await _client.GetAsync($"/api/v1/cases/{created.Id}");
        getResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
        var persisted = await getResponse.Content.ReadFromJsonAsync<CaseResponse>();
        persisted.ShouldNotBeNull();
        persisted.Status.ShouldBe("UnderScientificReview");
    }

    [Fact]
    public async Task StartScientificReview_WhenCaseUnknown_Returns404()
    {
        var response = await _client.PostAsync($"/api/v1/cases/{Guid.NewGuid()}/scientific-review", content: null);

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        response.Content.Headers.ContentType?.MediaType.ShouldBe("application/problem+json");
    }

    [Fact]
    public async Task StartScientificReview_WhenCaseIsDraft_Returns409()
    {
        var request = ValidRequest();
        var createResponse = await _client.PostAsJsonAsync("/api/v1/cases", request);
        createResponse.StatusCode.ShouldBe(HttpStatusCode.Created);
        var created = await createResponse.Content.ReadFromJsonAsync<CreateCaseResponse>();
        created.ShouldNotBeNull();

        var response = await _client.PostAsync($"/api/v1/cases/{created.Id}/scientific-review", content: null);

        response.StatusCode.ShouldBe(HttpStatusCode.Conflict);
        response.Content.Headers.ContentType?.MediaType.ShouldBe("application/problem+json");

        var body = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        body.ShouldNotBeNull();
        body.Detail.ShouldBe("Only a submitted case can enter review.");
    }

    [Fact]
    public async Task StartScientificReview_PersistsHistoryEntry()
    {
        var request = ValidRequest();
        var createResponse = await _client.PostAsJsonAsync("/api/v1/cases", request);
        createResponse.StatusCode.ShouldBe(HttpStatusCode.Created);
        var created = await createResponse.Content.ReadFromJsonAsync<CreateCaseResponse>();
        created.ShouldNotBeNull();

        await _client.PostAsync($"/api/v1/cases/{created.Id}/submission", content: null);
        await _client.PostAsync($"/api/v1/cases/{created.Id}/scientific-review", content: null);

        var historyResponse = await _client.GetAsync($"/api/v1/cases/{created.Id}/history");
        historyResponse.StatusCode.ShouldBe(HttpStatusCode.OK);

        var history = await historyResponse.Content.ReadFromJsonAsync<List<CaseStatusHistoryResponse>>();
        history.ShouldNotBeNull();
        history.ShouldContain(h => h.FromStatus == "Submitted" && h.ToStatus == "UnderScientificReview");
    }

    [Fact]
    public async Task GetCaseHistory_WhenCaseExists_ReturnsChronologicalHistory()
    {
        var request = ValidRequest();
        var createResponse = await _client.PostAsJsonAsync("/api/v1/cases", request);
        createResponse.StatusCode.ShouldBe(HttpStatusCode.Created);
        var created = await createResponse.Content.ReadFromJsonAsync<CreateCaseResponse>();
        created.ShouldNotBeNull();

        await _client.PostAsync($"/api/v1/cases/{created.Id}/submission", content: null);
        await _client.PostAsync($"/api/v1/cases/{created.Id}/scientific-review", content: null);

        var response = await _client.GetAsync($"/api/v1/cases/{created.Id}/history");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        response.Content.Headers.ContentType?.MediaType.ShouldBe("application/json");

        var history = await response.Content.ReadFromJsonAsync<List<CaseStatusHistoryResponse>>();
        history.ShouldNotBeNull();
        history.Count.ShouldBe(2);
        history[0].FromStatus.ShouldBe("Draft");
        history[0].ToStatus.ShouldBe("Submitted");
        history[^1].FromStatus.ShouldBe("Submitted");
        history[^1].ToStatus.ShouldBe("UnderScientificReview");
        for (var i = 1; i < history.Count; i++)
        {
            history[i].TransitionedAt.ShouldBeGreaterThanOrEqualTo(history[i - 1].TransitionedAt);
        }
    }

    [Fact]
    public async Task GetCaseHistory_WhenCaseUnknown_Returns404()
    {
        var response = await _client.GetAsync($"/api/v1/cases/{Guid.NewGuid()}/history");

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        response.Content.Headers.ContentType?.MediaType.ShouldBe("application/problem+json");
    }

    [Fact]
    public async Task FullScientificReviewTrack_WhenApproved_EndsInApprovedStatus()
    {
        var created = await CreateCaseAsync();
        await BringCaseToStateAsync(created.Id, CaseStatus.Approved);

        var getResponse = await _client.GetAsync($"/api/v1/cases/{created.Id}");
        getResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
        var body = await getResponse.Content.ReadFromJsonAsync<CaseResponse>();
        body.ShouldNotBeNull();
        body.Status.ShouldBe("Approved");

        var historyResponse = await _client.GetAsync($"/api/v1/cases/{created.Id}/history");
        historyResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
        var history = await historyResponse.Content.ReadFromJsonAsync<List<CaseStatusHistoryResponse>>();
        history.ShouldNotBeNull();
        history.Count.ShouldBe(5);
    }

    [Fact]
    public async Task FullScientificReviewTrack_WhenRejected_EndsInRejectedStatus()
    {
        var created = await CreateCaseAsync();
        await BringCaseToStateAsync(created.Id, CaseStatus.Rejected);

        var getResponse = await _client.GetAsync($"/api/v1/cases/{created.Id}");
        getResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
        var body = await getResponse.Content.ReadFromJsonAsync<CaseResponse>();
        body.ShouldNotBeNull();
        body.Status.ShouldBe("Rejected");
    }

    [Fact]
    public async Task StartLegalReview_WhenCaseIsSubmitted_Returns409()
    {
        var created = await CreateCaseAsync();
        await BringCaseToStateAsync(created.Id, CaseStatus.Submitted);

        var response = await _client.PostAsync($"/api/v1/cases/{created.Id}/legal-review", content: null);

        response.StatusCode.ShouldBe(HttpStatusCode.Conflict);
        response.Content.Headers.ContentType?.MediaType.ShouldBe("application/problem+json");
        var body = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        body.ShouldNotBeNull();
        body.Detail.ShouldBe("Only a case under scientific review can start legal review.");
    }

    [Fact]
    public async Task RequestDecision_WhenCaseIsUnderScientificReview_Returns409()
    {
        var created = await CreateCaseAsync();
        await BringCaseToStateAsync(created.Id, CaseStatus.UnderScientificReview);

        var response = await _client.PostAsync($"/api/v1/cases/{created.Id}/decision-request", content: null);

        response.StatusCode.ShouldBe(HttpStatusCode.Conflict);
        response.Content.Headers.ContentType?.MediaType.ShouldBe("application/problem+json");
        var body = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        body.ShouldNotBeNull();
        body.Detail.ShouldBe("Only a case under legal review can be advanced to decision.");
    }

    [Fact]
    public async Task Approve_WhenCaseIsUnderLegalReview_Returns409()
    {
        var created = await CreateCaseAsync();
        await BringCaseToStateAsync(created.Id, CaseStatus.UnderLegalReview);

        var response = await _client.PostAsync($"/api/v1/cases/{created.Id}/approval", content: null);

        response.StatusCode.ShouldBe(HttpStatusCode.Conflict);
        response.Content.Headers.ContentType?.MediaType.ShouldBe("application/problem+json");
        var body = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        body.ShouldNotBeNull();
        body.Detail.ShouldBe("Only a case pending decision can be approved or rejected.");
    }

    [Fact]
    public async Task Reject_WhenCaseIsUnderLegalReview_Returns409()
    {
        var created = await CreateCaseAsync();
        await BringCaseToStateAsync(created.Id, CaseStatus.UnderLegalReview);

        var response = await _client.PostAsync($"/api/v1/cases/{created.Id}/rejection", content: null);

        response.StatusCode.ShouldBe(HttpStatusCode.Conflict);
        response.Content.Headers.ContentType?.MediaType.ShouldBe("application/problem+json");
        var body = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        body.ShouldNotBeNull();
        body.Detail.ShouldBe("Only a case pending decision can be approved or rejected.");
    }

    [Theory]
    [InlineData("/legal-review")]
    [InlineData("/decision-request")]
    [InlineData("/approval")]
    [InlineData("/rejection")]
    public async Task TransitionEndpoint_WhenCaseUnknown_Returns404(string segment)
    {
        var response = await _client.PostAsync($"/api/v1/cases/{Guid.NewGuid()}{segment}", content: null);

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        response.Content.Headers.ContentType?.MediaType.ShouldBe("application/problem+json");
    }
}
