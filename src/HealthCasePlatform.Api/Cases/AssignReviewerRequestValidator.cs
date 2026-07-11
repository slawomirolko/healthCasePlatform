using FluentValidation;

namespace HealthCasePlatform.Api.Cases;

public sealed class AssignReviewerRequestValidator : AbstractValidator<AssignReviewerRequest>
{
    public AssignReviewerRequestValidator()
    {
        RuleFor(x => x.ReviewerId)
            .NotEmpty()
            .MaximumLength(100);
    }
}
