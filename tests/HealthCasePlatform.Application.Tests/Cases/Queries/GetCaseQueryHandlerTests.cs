using HealthCasePlatform.Application.Cases.Queries;
using HealthCasePlatform.Domain.Cases;
using NSubstitute;
using Shouldly;

namespace HealthCasePlatform.Application.Tests.Cases.Queries;

public sealed class GetCaseQueryHandlerTests : CaseHandlerTestBase
{
    [Fact]
    public async Task Handle_WhenCaseExists_ReturnsCase()
    {
        var existing = CreateCase();
        var repo = CreateRepository();
        repo.FindByIdReadOnlyAsync(existing.Id, Arg.Any<CancellationToken>()).Returns(existing);
        var handler = new GetCaseQueryHandler(repo);

        var result = await handler.Handle(new GetCaseQuery(existing.Id), CancellationToken.None);

        result.ShouldNotBeNull();
        result.Id.ShouldBe(existing.Id);
    }

    [Fact]
    public async Task Handle_WhenCaseNotFound_ReturnsNull()
    {
        var id = Guid.CreateVersion7();
        var repo = CreateRepository();
        repo.FindByIdReadOnlyAsync(id, Arg.Any<CancellationToken>()).Returns((RegulatoryCase?)null);
        var handler = new GetCaseQueryHandler(repo);

        var result = await handler.Handle(new GetCaseQuery(id), CancellationToken.None);

        result.ShouldBeNull();
    }
}
