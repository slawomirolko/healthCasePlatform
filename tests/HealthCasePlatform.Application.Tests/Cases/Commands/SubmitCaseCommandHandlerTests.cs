using HealthCasePlatform.Application.Cases;
using HealthCasePlatform.Application.Cases.Commands;
using HealthCasePlatform.Domain.Cases;
using HealthCasePlatform.Domain.Enums;
using NSubstitute;
using Shouldly;

namespace HealthCasePlatform.Application.Tests.Cases.Commands;

public sealed class SubmitCaseCommandHandlerTests : CaseHandlerTestBase
{
    private static readonly Guid CaseId = Guid.CreateVersion7();

    private static SubmitCaseCommand Command() => new(CaseId);

    [Fact]
    public async Task Handle_WhenCaseNotFound_ReturnsNotFound()
    {
        var repo = CreateRepository();
        repo.FindByIdAsync(CaseId, Arg.Any<CancellationToken>()).Returns((RegulatoryCase?)null);
        var handler = new SubmitCaseCommandHandler(repo);

        var result = await handler.Handle(Command(), CancellationToken.None);

        result.IsError.ShouldBeTrue();
        result.Errors.ShouldContain(CaseErrors.NotFound);
        await repo.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenCaseIsDraft_ReturnsSubmittedCase()
    {
        var existing = BringCaseTo(CaseStatus.Draft);
        var repo = CreateRepository();
        repo.FindByIdAsync(CaseId, Arg.Any<CancellationToken>()).Returns(existing);
        var handler = new SubmitCaseCommandHandler(repo);

        var result = await handler.Handle(Command(), CancellationToken.None);

        result.IsError.ShouldBeFalse();
        result.Value.Status.ShouldBe(CaseStatus.Submitted);
        await repo.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenCaseAlreadySubmitted_ReturnsConflictError()
    {
        var existing = BringCaseTo(CaseStatus.Submitted);
        var repo = CreateRepository();
        repo.FindByIdAsync(CaseId, Arg.Any<CancellationToken>()).Returns(existing);
        var handler = new SubmitCaseCommandHandler(repo);

        var result = await handler.Handle(Command(), CancellationToken.None);

        result.IsError.ShouldBeTrue();
        result.Errors.ShouldContain(e => e.Code == RegulatoryCaseErrors.NotDraft.Code);
    }

    [Fact]
    public async Task Handle_WhenTransitionFails_DoesNotPersist()
    {
        var existing = BringCaseTo(CaseStatus.Submitted);
        var repo = CreateRepository();
        repo.FindByIdAsync(CaseId, Arg.Any<CancellationToken>()).Returns(existing);
        var handler = new SubmitCaseCommandHandler(repo);

        var result = await handler.Handle(Command(), CancellationToken.None);

        result.IsError.ShouldBeTrue();
        await repo.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
