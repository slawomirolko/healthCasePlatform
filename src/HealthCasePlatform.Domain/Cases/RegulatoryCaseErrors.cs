using ErrorOr;

namespace HealthCasePlatform.Domain.Cases;

public static class RegulatoryCaseErrors
{
    public static readonly Error TitleEmpty = Error.Validation("RegulatoryCase.TitleEmpty", "Case title cannot be empty.");
    public static readonly Error CaseTypeIdEmpty = Error.Validation("RegulatoryCase.CaseTypeIdEmpty", "Case type ID cannot be empty.");
    public static readonly Error CreatedByEmpty = Error.Validation("RegulatoryCase.CreatedByEmpty", "Created by cannot be empty.");
    public static readonly Error CountryEmpty = Error.Validation("RegulatoryCase.CountryEmpty", "Country cannot be empty.");
    public static readonly Error CountryInvalid = Error.Validation("RegulatoryCase.CountryInvalid", "Country must be a 2-letter ISO code.");

    public static readonly Error NotDraft = Error.Conflict("RegulatoryCase.NotDraft", "Only a draft case can be submitted.");
    public static readonly Error NotSubmitted = Error.Conflict("RegulatoryCase.NotSubmitted", "Only a submitted case can enter review.");
    public static readonly Error NotUnderScientificReview = Error.Conflict("RegulatoryCase.NotUnderScientificReview", "Only a case under scientific review can start legal review.");
    public static readonly Error NotUnderLegalReview = Error.Conflict("RegulatoryCase.NotUnderLegalReview", "Only a case under legal review can be advanced to decision.");
    public static readonly Error NotPendingDecision = Error.Conflict("RegulatoryCase.NotPendingDecision", "Only a case pending decision can be approved or rejected.");
    public static readonly Error InTerminalState = Error.Conflict("RegulatoryCase.InTerminalState", "Cannot change status of a case in a terminal state.");

    public static readonly Error DocumentNull = Error.Validation("RegulatoryCase.DocumentNull", "Document cannot be null.");
    public static readonly Error TaskNull = Error.Validation("RegulatoryCase.TaskNull", "Task cannot be null.");
    public static readonly Error CommentNull = Error.Validation("RegulatoryCase.CommentNull", "Comment cannot be null.");
    public static readonly Error DecisionNull = Error.Validation("RegulatoryCase.DecisionNull", "Decision cannot be null.");
    public static readonly Error ReviewerIdEmpty = Error.Validation("RegulatoryCase.ReviewerIdEmpty", "Reviewer id cannot be empty.");
}
