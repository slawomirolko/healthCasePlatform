using FluentValidation;

namespace HealthCasePlatform.Api.Cases;

public sealed class CreateCaseRequestValidator : AbstractValidator<CreateCaseRequest>
{
    public CreateCaseRequestValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty();

        RuleFor(x => x.CaseTypeId)
            .NotEmpty();

        RuleFor(x => x.CreatedBy)
            .NotEmpty();

        RuleFor(x => x.Country)
            .NotEmpty()
            .Matches(@"^[A-Za-z]{2}$")
            .WithMessage("'Country' must be a 2-letter ISO code.");
    }
}
