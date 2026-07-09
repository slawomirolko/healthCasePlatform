using HealthCasePlatform.Application.Cases.Queries;
using HealthCasePlatform.Domain.Cases;
using HealthCasePlatform.Domain.Enums;
using NSubstitute;
using Shouldly;

namespace HealthCasePlatform.Application.Tests.Cases.Queries;

public sealed class ListCasesQueryHandlerTests : CaseHandlerTestBase
{
    private void SetupList(
        ICaseRepository repo,
        IReadOnlyList<RegulatoryCase> items,
        int totalCount)
    {
        repo.ListAsync(
            Arg.Any<CaseStatus?>(),
            Arg.Any<CasePriority?>(),
            Arg.Any<string?>(),
            Arg.Any<int>(),
            Arg.Any<int>(),
            Arg.Any<CancellationToken>())
            .Returns((items, totalCount));
    }

    [Theory]
    [InlineData(0, 10, 1, 10)]
    [InlineData(5, 999, 5, 100)]
    public async Task Handle_ClampsPaginationBoundsToValidRange(
        int page, int pageSize, int expectedPage, int expectedPageSize)
    {
        var repo = CreateRepository();
        SetupList(repo, Array.Empty<RegulatoryCase>(), 0);
        var handler = new ListCasesQueryHandler(repo);

        var result = await handler.Handle(
            new ListCasesQuery(null, null, null, page, pageSize), CancellationToken.None);

        result.Page.ShouldBe(expectedPage);
        result.PageSize.ShouldBe(expectedPageSize);
    }

    [Fact]
    public async Task Handle_NormalizesCountryToUpperCase()
    {
        string? capturedCountry = null;
        var repo = CreateRepository();
        repo.ListAsync(
            Arg.Any<CaseStatus?>(),
            Arg.Any<CasePriority?>(),
            Arg.Do<string?>(c => capturedCountry = c),
            Arg.Any<int>(),
            Arg.Any<int>(),
            Arg.Any<CancellationToken>())
            .Returns((Array.Empty<RegulatoryCase>(), 0));
        var handler = new ListCasesQueryHandler(repo);

        await handler.Handle(new ListCasesQuery(null, null, "pl", 1, 10), CancellationToken.None);

        capturedCountry.ShouldBe("PL");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("  ")]
    public async Task Handle_WhenCountryBlank_PassesNullToRepository(string? country)
    {
        string? capturedCountry = "unset";
        var repo = CreateRepository();
        repo.ListAsync(
            Arg.Any<CaseStatus?>(),
            Arg.Any<CasePriority?>(),
            Arg.Do<string?>(c => capturedCountry = c),
            Arg.Any<int>(),
            Arg.Any<int>(),
            Arg.Any<CancellationToken>())
            .Returns((Array.Empty<RegulatoryCase>(), 0));
        var handler = new ListCasesQueryHandler(repo);

        await handler.Handle(new ListCasesQuery(null, null, country, 1, 10), CancellationToken.None);

        capturedCountry.ShouldBeNull();
    }

    [Fact]
    public async Task Handle_ReturnsPagedResultWithCalculatedTotalPages()
    {
        var items = new[] { CreateCase(), CreateCase() };
        var repo = CreateRepository();
        SetupList(repo, items, 3);
        var handler = new ListCasesQueryHandler(repo);

        var result = await handler.Handle(new ListCasesQuery(null, null, null, 1, 2), CancellationToken.None);

        result.TotalCount.ShouldBe(3);
        result.TotalPages.ShouldBe(2);
        result.Items.Count.ShouldBe(2);
    }

    [Fact]
    public async Task Handle_ForwardsAllFilterParametersToRepository()
    {
        CaseStatus? capturedStatus = null;
        CasePriority? capturedPriority = null;
        string? capturedCountry = null;
        var capturedPage = 0;
        var capturedPageSize = 0;
        var repo = CreateRepository();
        repo.ListAsync(
            Arg.Do<CaseStatus?>(s => capturedStatus = s),
            Arg.Do<CasePriority?>(p => capturedPriority = p),
            Arg.Do<string?>(c => capturedCountry = c),
            Arg.Do<int>(pg => capturedPage = pg),
            Arg.Do<int>(ps => capturedPageSize = ps),
            Arg.Any<CancellationToken>())
            .Returns((Array.Empty<RegulatoryCase>(), 0));
        var handler = new ListCasesQueryHandler(repo);

        await handler.Handle(
            new ListCasesQuery(CaseStatus.Submitted, CasePriority.High, "pl", 2, 25),
            CancellationToken.None);

        capturedStatus.ShouldBe(CaseStatus.Submitted);
        capturedPriority.ShouldBe(CasePriority.High);
        capturedCountry.ShouldBe("PL");
        capturedPage.ShouldBe(2);
        capturedPageSize.ShouldBe(25);
    }
}
