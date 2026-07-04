using ErrorOr;

namespace HealthCasePlatform.Domain.Cases;

public static class CaseDocumentErrors
{
    public static readonly Error CaseIdEmpty = Error.Validation("CaseDocument.CaseIdEmpty", "Case ID cannot be empty.");
    public static readonly Error FileNameEmpty = Error.Validation("CaseDocument.FileNameEmpty", "File name cannot be empty.");
    public static readonly Error BlobReferenceEmpty = Error.Validation("CaseDocument.BlobReferenceEmpty", "Blob reference cannot be empty.");
    public static readonly Error UploadedByEmpty = Error.Validation("CaseDocument.UploadedByEmpty", "Uploaded by cannot be empty.");
}
