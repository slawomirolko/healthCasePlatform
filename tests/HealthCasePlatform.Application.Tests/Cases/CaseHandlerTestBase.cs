using HealthCasePlatform.Domain.Cases;
using HealthCasePlatform.Domain.Enums;
using NSubstitute;
using Shouldly;

namespace HealthCasePlatform.Application.Tests.Cases;

public abstract class CaseHandlerTestBase
{
    protected static ICaseRepository CreateRepository() => Substitute.For<ICaseRepository>();

    protected static IAuditLogWriter CreateAuditWriter() => Substitute.For<IAuditLogWriter>();

    protected static RegulatoryCase CreateCase() =>
        RegulatoryCase.Create("Food safety incident #42", "Initial report", Guid.NewGuid(), CasePriority.High, "officer-1", "PL").Value;

    protected static RegulatoryCase BringCaseTo(CaseStatus target)
    {
        var sut = CreateCase();
        if (target == CaseStatus.Draft)
        {
            return sut;
        }

        sut.Submit().IsError.ShouldBeFalse();
        if (target == CaseStatus.Submitted)
        {
            return sut;
        }

        sut.StartScientificReview().IsError.ShouldBeFalse();
        if (target == CaseStatus.UnderScientificReview)
        {
            return sut;
        }

        sut.StartLegalReview().IsError.ShouldBeFalse();
        if (target == CaseStatus.UnderLegalReview)
        {
            return sut;
        }

        sut.RequestDecision().IsError.ShouldBeFalse();
        if (target == CaseStatus.PendingDecision)
        {
            return sut;
        }

        if (target == CaseStatus.Approved)
        {
            sut.Approve().IsError.ShouldBeFalse();
            return sut;
        }

        if (target == CaseStatus.Rejected)
        {
            sut.Reject().IsError.ShouldBeFalse();
            return sut;
        }

        throw new ArgumentOutOfRangeException(nameof(target), $"Unsupported target status for test factory: {target}");
    }
}
