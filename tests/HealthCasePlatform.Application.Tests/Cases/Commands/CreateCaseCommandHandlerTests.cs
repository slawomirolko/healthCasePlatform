using HealthCasePlatform.Application.Cases.Commands;
using HealthCasePlatform.Domain.Cases;
using HealthCasePlatform.Domain.Enums;
using NSubstitute;
using Shouldly;

namespace HealthCasePlatform.Application.Tests.Cases.Commands;

public sealed class CreateCaseCommandHandlerTests : CaseHandlerTestBase
{
    private static CreateCaseCommand ValidCommand() => new(
        "Food safety incident #42",
        "Initial report",
        Guid.NewGuid(),
        CasePriority.High,
        "officer-1",
        "PL");

    [Fact]
    public async Task Handle_WithValidCommand_ReturnsCreatedCase()
    {
        var repo = CreateRepository();
        var handler = new CreateCaseCommandHandler(repo);
        var command = ValidCommand();

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsError.ShouldBeFalse();
        result.Value.Title.ShouldBe(command.Title);
        result.Value.Country.ShouldBe("PL");
        result.Value.Status.ShouldBe(CaseStatus.Draft);
        await repo.Received(1).AddAsync(result.Value, Arg.Any<CancellationToken>());
        await repo.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenTitleEmpty_ReturnsValidationErrors()
    {
        var repo = CreateRepository();
        var handler = new CreateCaseCommandHandler(repo);
        var command = ValidCommand() with { Title = "" };

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsError.ShouldBeTrue();
    }

    [Fact]
    public async Task Handle_WhenDomainCreateFails_DoesNotPersist()
    {
        var repo = CreateRepository();
        var handler = new CreateCaseCommandHandler(repo);
        var command = ValidCommand() with { Title = "" };

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsError.ShouldBeTrue();
        await repo.DidNotReceive().AddAsync(Arg.Any<RegulatoryCase>(), Arg.Any<CancellationToken>());
        await repo.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
