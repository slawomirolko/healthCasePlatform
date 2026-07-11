using System.Net;
using HealthCasePlatform.Api.Cases;
using HealthCasePlatform.Domain.Cases;
using HealthCasePlatform.Domain.Enums;
using HealthCasePlatform.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;

namespace HealthCasePlatform.Api.Tests.Integration;

public sealed class CaseSubmittedNotificationTests : IClassFixture<ApiFactory>
{
    private readonly HttpClient _client;
    private readonly ApiFactory _factory;

    public CaseSubmittedNotificationTests(ApiFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    private static async Task<List<Notification>> FindNotificationsAsync(ApiFactory factory, Guid caseId)
    {
        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        return await db.Notifications.AsNoTracking().Where(n => n.CaseId == caseId).ToListAsync();
    }

    [Fact]
    public async Task SubmitCase_OnSuccess_CreatesNotificationRecord()
    {
        var created = await _client.CreateCaseAsync();

        var response = await _client.PostAsync($"/api/v1/cases/{created.Id}/submission", content: null);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var notifications = await FindNotificationsAsync(_factory, created.Id);
        var notification = notifications.ShouldHaveSingleItem();
        notification.Type.ShouldBe(NotificationType.CaseSubmitted);
        notification.CaseId.ShouldBe(created.Id);
        notification.CreatedAt.ShouldBeGreaterThan(DateTime.MinValue);
    }

    [Fact]
    public async Task SubmitCase_WhenTransitionFails_DoesNotCreateNotificationRecord()
    {
        var created = await _client.CreateCaseAsync();
        await _client.PostAsync($"/api/v1/cases/{created.Id}/submission", content: null);

        var secondSubmit = await _client.PostAsync($"/api/v1/cases/{created.Id}/submission", content: null);

        secondSubmit.StatusCode.ShouldBe(HttpStatusCode.Conflict);
        var notifications = await FindNotificationsAsync(_factory, created.Id);
        notifications.Count.ShouldBe(1);
    }

    [Fact]
    public async Task CreateCase_WithoutSubmit_DoesNotCreateNotificationRecord()
    {
        var created = await _client.CreateCaseAsync();

        var notifications = await FindNotificationsAsync(_factory, created.Id);

        notifications.ShouldBeEmpty();
    }
}
