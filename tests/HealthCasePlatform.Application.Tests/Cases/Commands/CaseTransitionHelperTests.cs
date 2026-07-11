using HealthCasePlatform.Application.Cases;
using HealthCasePlatform.Application.Cases.Commands;
using HealthCasePlatform.Domain.Cases;
using HealthCasePlatform.Domain.Enums;
using NSubstitute;
using Shouldly;

namespace HealthCasePlatform.Application.Tests.Cases.Commands;

public sealed class CaseTransitionHelperTests : CaseHandlerTestBase
{
    private const string Actor = "officer-1";

    [Fact]
    public async Task Transition_Submit_StagesStatusChangedAuditEntry()
    {
        var existing = BringCaseTo(CaseStatus.Draft);
        var repo = CreateRepository();
        var writer = CreateAuditWriter();
        repo.FindByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(existing);

        await CaseTransitionHelper.TransitionAsync(
            repo, writer, existing.Id, c => c.Submit(), CancellationToken.None, Actor, AuditAction.StatusChanged);

        await writer.Received(1).WriteAsync(
            Arg.Is<AuditEntry>(a => a.Action == AuditAction.StatusChanged
                                    && a.Actor == Actor
                                    && a.Detail == "Draft → Submitted"),
            Arg.Any<CancellationToken>());
        Received.InOrder(async () =>
        {
            await writer.WriteAsync(Arg.Any<AuditEntry>(), Arg.Any<CancellationToken>());
            await repo.SaveChangesAsync(Arg.Any<CancellationToken>());
        });
    }

    [Fact]
    public async Task Transition_Approve_StagesDecisionMadeAuditEntry()
    {
        var existing = BringCaseTo(CaseStatus.PendingDecision);
        var repo = CreateRepository();
        var writer = CreateAuditWriter();
        repo.FindByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(existing);

        await CaseTransitionHelper.TransitionAsync(
            repo, writer, existing.Id, c => c.Approve(), CancellationToken.None, Actor, AuditAction.DecisionMade);

        await writer.Received(1).WriteAsync(
            Arg.Is<AuditEntry>(a => a.Action == AuditAction.DecisionMade
                                    && a.Detail == "PendingDecision → Approved"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Transition_WithNullActor_WritesNoAudit()
    {
        var existing = BringCaseTo(CaseStatus.Draft);
        var repo = CreateRepository();
        var writer = CreateAuditWriter();
        repo.FindByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(existing);

        await CaseTransitionHelper.TransitionAsync(
            repo, writer, existing.Id, c => c.Submit(), CancellationToken.None);

        await writer.DidNotReceive().WriteAsync(Arg.Any<AuditEntry>(), Arg.Any<CancellationToken>());
        await repo.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Transition_WhenDomainFails_WritesNoAudit()
    {
        var existing = BringCaseTo(CaseStatus.Submitted);
        var repo = CreateRepository();
        var writer = CreateAuditWriter();
        repo.FindByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(existing);

        var result = await CaseTransitionHelper.TransitionAsync(
            repo, writer, existing.Id, c => c.Submit(), CancellationToken.None, Actor, AuditAction.StatusChanged);

        result.IsError.ShouldBeTrue();
        await writer.DidNotReceive().WriteAsync(Arg.Any<AuditEntry>(), Arg.Any<CancellationToken>());
        await repo.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
