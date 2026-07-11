using System.Net;
using System.Net.Http.Json;
using HealthCasePlatform.Api.Cases;
using HealthCasePlatform.Api.Common;
using HealthCasePlatform.Domain.Enums;
using Shouldly;

namespace HealthCasePlatform.Api.Tests.Integration;

public sealed class AuditEndpointsTests : IClassFixture<ApiFactory>
{
    private readonly ApiFactory _factory;

    public AuditEndpointsTests(ApiFactory factory)
    {
        _factory = factory;
    }

    private HttpClient CreateAuditorClient() => _factory.CreateClientWithRoles(AppRoles.Auditor);
    private HttpClient CreateUnauthorizedClient() => _factory.CreateClientWithRoles(AppRoles.CaseOfficer);

    [Fact]
    public async Task CreateCase_OnSuccess_WritesCaseCreatedAuditEntry()
    {
        var client = _factory.CreateClient();
        var created = await client.CreateCaseAsync();

        var auditClient = CreateAuditorClient();
        var response = await auditClient.GetAsync($"/api/v1/cases/{created.Id}/audit");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var audit = await response.Content.ReadFromJsonAsync<List<AuditEntryResponse>>();
        audit.ShouldNotBeNull();
        audit.Count.ShouldBe(1);
        audit[0].Action.ShouldBe("CaseCreated");
        audit[0].Actor.ShouldBe("officer-1");
        audit[0].CaseId.ShouldBe(created.Id);
    }

    [Fact]
    public async Task SubmitCase_OnSuccess_WritesStatusChangedAuditEntry()
    {
        var client = _factory.CreateClient();
        var created = await client.CreateCaseAsync();
        await client.BringCaseToStateAsync(created.Id, CaseStatus.Submitted);

        var auditClient = CreateAuditorClient();
        var response = await auditClient.GetAsync($"/api/v1/cases/{created.Id}/audit");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var audit = await response.Content.ReadFromJsonAsync<List<AuditEntryResponse>>();
        audit.ShouldNotBeNull();
        audit.Count.ShouldBe(2);
        audit.ShouldContain(a => a.Action == "StatusChanged" && a.Detail == "Draft → Submitted");
    }

    [Fact]
    public async Task ApproveCase_OnSuccess_WritesDecisionMadeAuditEntry()
    {
        var client = _factory.CreateClient();
        var created = await client.CreateCaseAsync();
        await client.BringCaseToStateAsync(created.Id, CaseStatus.Approved);

        var auditClient = CreateAuditorClient();
        var response = await auditClient.GetAsync($"/api/v1/cases/{created.Id}/audit");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var audit = await response.Content.ReadFromJsonAsync<List<AuditEntryResponse>>();
        audit.ShouldNotBeNull();
        audit.ShouldContain(a => a.Action == "DecisionMade" && a.Detail != null && a.Detail.Contains("Approved"));
    }

    [Fact]
    public async Task Workflow_AccumulatesAuditEntriesInOrder()
    {
        var client = _factory.CreateClient();
        var created = await client.CreateCaseAsync();
        await client.BringCaseToStateAsync(created.Id, CaseStatus.Approved);

        var auditClient = CreateAuditorClient();
        var response = await auditClient.GetAsync($"/api/v1/cases/{created.Id}/audit");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var audit = await response.Content.ReadFromJsonAsync<List<AuditEntryResponse>>();
        audit.ShouldNotBeNull();
        audit.Count.ShouldBe(6);
        for (var i = 1; i < audit.Count; i++)
        {
            audit[i].OccurredAt.ShouldBeGreaterThanOrEqualTo(audit[i - 1].OccurredAt);
        }
    }

    [Fact]
    public async Task Transition_WhenDomainFails_WritesNoAuditEntry()
    {
        var client = _factory.CreateClient();
        var created = await client.CreateCaseAsync();
        await client.BringCaseToStateAsync(created.Id, CaseStatus.Submitted);

        var secondSubmit = await client.PostAsync($"/api/v1/cases/{created.Id}/submission", content: null);
        secondSubmit.StatusCode.ShouldBe(HttpStatusCode.Conflict);

        var auditClient = CreateAuditorClient();
        var response = await auditClient.GetAsync($"/api/v1/cases/{created.Id}/audit");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var audit = await response.Content.ReadFromJsonAsync<List<AuditEntryResponse>>();
        audit.ShouldNotBeNull();
        audit.Count.ShouldBe(2);
        audit.ShouldContain(a => a.Action == "StatusChanged" && a.Detail == "Draft → Submitted");
        audit.ShouldNotContain(a => a.Action == "StatusChanged" && a.Detail == "Submitted → Submitted");
    }

    [Fact]
    public async Task GetAudit_WhenCaseMissing_Returns404()
    {
        var auditClient = CreateAuditorClient();

        var response = await auditClient.GetAsync($"/api/v1/cases/{Guid.NewGuid()}/audit");

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        response.Content.Headers.ContentType?.MediaType.ShouldBe("application/problem+json");
    }

    [Fact]
    public async Task GetAudit_AsAuditorRole_Returns200()
    {
        var client = _factory.CreateClient();
        var created = await client.CreateCaseAsync();

        var response = await CreateAuditorClient().GetAsync($"/api/v1/cases/{created.Id}/audit");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetAudit_AsUnauthorizedRole_Returns403()
    {
        var client = _factory.CreateClient();
        var created = await client.CreateCaseAsync();

        var response = await CreateUnauthorizedClient().GetAsync($"/api/v1/cases/{created.Id}/audit");

        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
        response.Content.Headers.ContentType?.MediaType.ShouldBe("application/problem+json");
    }
}
