using HealthCasePlatform.Application.Cases;
using HealthCasePlatform.Application.Cases.Commands;
using HealthCasePlatform.Domain.Cases;
using HealthCasePlatform.Domain.Enums;
using NSubstitute;
using Shouldly;

namespace HealthCasePlatform.Application.Tests.Cases.Commands;

public sealed class StartLegalReviewCommandHandlerTests : CaseHandlerTestBase
{
    private static readonly Guid CaseId = Guid.CreateVersion7();

    private static StartLegalReviewCommand Command() => new(CaseId);

    [Fact]
    public async Task Handle_WhenCaseNotFound_ReturnsNotFound()
    {
        var repo = CreateRepository();
        repo.FindByIdAsync(CaseId, Arg.Any<CancellationToken>()).Returns((RegulatoryCase?)null);
        var handler = new StartLegalReviewCommandHandler(repo, CreateAuditWriter());

        var result = await handler.Handle(Command(), CancellationToken.None);

        result.IsError.ShouldBeTrue();
        result.Errors.ShouldContain(CaseErrors.NotFound);
        await repo.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenCaseIsUnderScientificReview_ReturnsUnderLegalReviewCase()
    {
        var existing = BringCaseTo(CaseStatus.UnderScientificReview);
        var repo = CreateRepository();
        repo.FindByIdAsync(CaseId, Arg.Any<CancellationToken>()).Returns(existing);
        var handler = new StartLegalReviewCommandHandler(repo, CreateAuditWriter());

        var result = await handler.Handle(Command(), CancellationToken.None);

        result.IsError.ShouldBeFalse();
        result.Value.Status.ShouldBe(CaseStatus.UnderLegalReview);
        await repo.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
