using FluentValidation;

namespace HealthCasePlatform.Api.Cases;

public sealed class CreateCaseRequestValidator : AbstractValidator<CreateCaseRequest>
{
    public CreateCaseRequestValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty()
            .MaximumLength(200);

        RuleFor(x => x.Description)
            .MaximumLength(2000);

        RuleFor(x => x.CaseTypeId)
            .NotEmpty();

        RuleFor(x => x.CreatedBy)
            .NotEmpty()
            .MaximumLength(100);

        RuleFor(x => x.Country)
            .NotEmpty()
            .Matches(@"^[A-Za-z]{2}$")
            .WithMessage("'Country' must be a 2-letter ISO code.");

        RuleFor(x => x.Priority)
            .IsInEnum();
    }
}
