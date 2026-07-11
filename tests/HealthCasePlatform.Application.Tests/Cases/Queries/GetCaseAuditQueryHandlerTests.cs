using HealthCasePlatform.Application.Cases;
using HealthCasePlatform.Application.Cases.Queries;
using HealthCasePlatform.Domain.Cases;
using HealthCasePlatform.Domain.Enums;
using NSubstitute;
using Shouldly;

namespace HealthCasePlatform.Application.Tests.Cases.Queries;

public sealed class GetCaseAuditQueryHandlerTests : CaseHandlerTestBase
{
    [Fact]
    public async Task Handle_WhenCaseMissing_ReturnsNotFound()
    {
        var id = Guid.CreateVersion7();
        var repo = CreateRepository();
        var writer = CreateAuditWriter();
        repo.ExistsAsync(id, Arg.Any<CancellationToken>()).Returns(false);
        var handler = new GetCaseAuditQueryHandler(repo, writer);

        var result = await handler.Handle(new GetCaseAuditQuery(id), CancellationToken.None);

        result.IsError.ShouldBeTrue();
        result.Errors.ShouldContain(CaseErrors.NotFound);
        await writer.DidNotReceive().GetByCaseIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_OnSuccess_ReturnsEntries()
    {
        var existing = CreateCase();
        var entries = new List<AuditEntry>
        {
            AuditEntry.Create(existing.Id, AuditAction.CaseCreated, "officer-1", existing.Title).Value
        };
        var repo = CreateRepository();
        var writer = CreateAuditWriter();
        repo.ExistsAsync(existing.Id, Arg.Any<CancellationToken>()).Returns(true);
        writer.GetByCaseIdAsync(existing.Id, Arg.Any<CancellationToken>()).Returns(entries);
        var handler = new GetCaseAuditQueryHandler(repo, writer);

        var result = await handler.Handle(new GetCaseAuditQuery(existing.Id), CancellationToken.None);

        result.IsError.ShouldBeFalse();
        result.Value.ShouldBe(entries);
    }
}
