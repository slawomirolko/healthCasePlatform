using HealthCasePlatform.Application.Cases;
using HealthCasePlatform.Application.Cases.Commands;
using HealthCasePlatform.Domain.Cases;
using HealthCasePlatform.Domain.Enums;
using NSubstitute;
using Shouldly;

namespace HealthCasePlatform.Application.Tests.Cases.Commands;

public sealed class AssignScientificReviewerCommandHandlerTests : CaseHandlerTestBase
{
    private static readonly Guid CaseId = Guid.CreateVersion7();

    private static AssignScientificReviewerCommand Command() => new(CaseId, "sci-1");

    [Fact]
    public async Task Handle_WhenCaseNotFound_ReturnsNotFound()
    {
        var repo = CreateRepository();
        repo.FindByIdAsync(CaseId, Arg.Any<CancellationToken>()).Returns((RegulatoryCase?)null);
        var handler = new AssignScientificReviewerCommandHandler(repo, CreateAuditWriter());

        var result = await handler.Handle(Command(), CancellationToken.None);

        result.IsError.ShouldBeTrue();
        result.Errors.ShouldContain(CaseErrors.NotFound);
        await repo.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenReviewerIdValid_AssignsAndSaves()
    {
        var existing = BringCaseTo(CaseStatus.Draft);
        var repo = CreateRepository();
        repo.FindByIdAsync(CaseId, Arg.Any<CancellationToken>()).Returns(existing);
        var handler = new AssignScientificReviewerCommandHandler(repo, CreateAuditWriter());

        var result = await handler.Handle(Command(), CancellationToken.None);

        result.IsError.ShouldBeFalse();
        result.Value.AssignedScientificReviewerId.ShouldBe("sci-1");
        await repo.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenReviewerIdEmpty_DoesNotPersist()
    {
        var existing = BringCaseTo(CaseStatus.Draft);
        var repo = CreateRepository();
        repo.FindByIdAsync(CaseId, Arg.Any<CancellationToken>()).Returns(existing);
        var handler = new AssignScientificReviewerCommandHandler(repo, CreateAuditWriter());

        var result = await handler.Handle(new AssignScientificReviewerCommand(CaseId, "  "), CancellationToken.None);

        result.IsError.ShouldBeTrue();
        result.Errors.ShouldContain(e => e.Code == RegulatoryCaseErrors.ReviewerIdEmpty.Code);
        await repo.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
