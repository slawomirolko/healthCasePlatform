using ErrorOr;
using HealthCasePlatform.Domain.Common;

namespace HealthCasePlatform.Domain.Cases;

public sealed class CaseDocument : Entity
{
    public Guid CaseId { get; private set; }
    public string FileName { get; private set; }
    public string ContentType { get; private set; }
    public string BlobReference { get; private set; }
    public string UploadedBy { get; private set; }
    public DateTime UploadedAt { get; private set; }

    private CaseDocument() { }

    public static ErrorOr<CaseDocument> Create(Guid caseId, string fileName, string contentType, string blobReference, string uploadedBy)
    {
        if (caseId == Guid.Empty)
        {
            return CaseDocumentErrors.CaseIdEmpty;
        }

        if (string.IsNullOrWhiteSpace(fileName))
        {
            return CaseDocumentErrors.FileNameEmpty;
        }

        if (string.IsNullOrWhiteSpace(blobReference))
        {
            return CaseDocumentErrors.BlobReferenceEmpty;
        }

        if (string.IsNullOrWhiteSpace(uploadedBy))
        {
            return CaseDocumentErrors.UploadedByEmpty;
        }

        return new CaseDocument
        {
            Id = Guid.CreateVersion7(),
            CaseId = caseId,
            FileName = fileName,
            ContentType = contentType,
            BlobReference = blobReference,
            UploadedBy = uploadedBy,
            UploadedAt = DateTime.UtcNow
        };
    }
}
