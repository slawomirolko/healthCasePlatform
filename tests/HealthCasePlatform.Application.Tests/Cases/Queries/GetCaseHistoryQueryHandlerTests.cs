using HealthCasePlatform.Application.Cases;
using HealthCasePlatform.Application.Cases.Queries;
using HealthCasePlatform.Domain.Cases;
using NSubstitute;
using Shouldly;

namespace HealthCasePlatform.Application.Tests.Cases.Queries;

public sealed class GetCaseHistoryQueryHandlerTests : CaseHandlerTestBase
{
    [Fact]
    public async Task Handle_WhenCaseNotFound_ReturnsNotFoundError()
    {
        var id = Guid.CreateVersion7();
        var repo = CreateRepository();
        repo.ExistsAsync(id, Arg.Any<CancellationToken>()).Returns(false);
        var handler = new GetCaseHistoryQueryHandler(repo);

        var result = await handler.Handle(new GetCaseHistoryQuery(id), CancellationToken.None);

        result.IsError.ShouldBeTrue();
        result.Errors.ShouldContain(CaseErrors.NotFound);
        await repo.DidNotReceive().GetHistoryByCaseIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenCaseExists_ReturnsHistoryFromRepository()
    {
        var existing = CreateCase();
        existing.Submit();
        var history = existing.History;
        var repo = CreateRepository();
        repo.ExistsAsync(existing.Id, Arg.Any<CancellationToken>()).Returns(true);
        repo.GetHistoryByCaseIdAsync(existing.Id, Arg.Any<CancellationToken>()).Returns(history);
        var handler = new GetCaseHistoryQueryHandler(repo);

        var result = await handler.Handle(new GetCaseHistoryQuery(existing.Id), CancellationToken.None);

        result.IsError.ShouldBeFalse();
        result.Value.ShouldBe(history);
    }
}
