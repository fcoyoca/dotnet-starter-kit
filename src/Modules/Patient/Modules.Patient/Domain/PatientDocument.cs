using FSH.Framework.Core.Domain;

namespace FSH.Modules.Patient.Domain;

/// <summary>
/// An uploaded patient chart document (legacy <c>PatientDocuments</c>: <c>pdFileName</c>/<c>pdPath</c>/
/// <c>pdPatientDocumentCategoryID</c>/<c>pdNotes</c>). The file itself lives on disk under a per-tenant
/// folder (see <c>IPatientDocumentStorage</c>, mirroring legacy <c>c_{clientID}/patientDocuments/</c>);
/// only the relative <see cref="StoredPath"/> is persisted. <see cref="DocumentTypeId"/> is a bare
/// cross-module id into Administration's PatientDocumentType lookup — no FK across schemas.
/// Soft-deletable (legacy pdDeletedByID/pdDeletedDate); the file is retained on delete, like legacy.
/// </summary>
public sealed class PatientDocument : AggregateRoot<Guid>, ISoftDeletable
{
    public Guid PatientId { get; private set; }
    public Guid? DocumentTypeId { get; private set; }
    public string FileName { get; private set; } = default!;
    public string StoredPath { get; private set; } = default!;
    public string ContentType { get; private set; } = default!;
    public long FileSizeBytes { get; private set; }
    public string? Notes { get; private set; }

    public string? UploadedByUserId { get; private set; }
    public string? UploadedByName { get; private set; }
    public DateTime UploadedAtUtc { get; private set; }
    public string? UpdatedByUserId { get; private set; }
    public string? UpdatedByName { get; private set; }
    public DateTime? UpdatedAtUtc { get; private set; }

    public bool IsDeleted { get; private set; }
    public DateTimeOffset? DeletedOnUtc { get; private set; }
    public string? DeletedBy { get; private set; }

    private PatientDocument() { }

    public static PatientDocument Create(
        Guid patientId,
        Guid? documentTypeId,
        string fileName,
        string storedPath,
        string contentType,
        long fileSizeBytes,
        string? notes,
        string? uploadedByUserId,
        string? uploadedByName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(fileName);
        ArgumentException.ThrowIfNullOrWhiteSpace(storedPath);
        ArgumentException.ThrowIfNullOrWhiteSpace(contentType);
        return new PatientDocument
        {
            Id = Guid.CreateVersion7(),
            PatientId = patientId,
            DocumentTypeId = documentTypeId,
            FileName = fileName.Trim(),
            StoredPath = storedPath,
            ContentType = contentType,
            FileSizeBytes = fileSizeBytes,
            Notes = string.IsNullOrWhiteSpace(notes) ? null : notes.Trim(),
            UploadedByUserId = uploadedByUserId,
            UploadedByName = uploadedByName,
            UploadedAtUtc = DateTime.UtcNow
        };
    }

    public void UpdateDetails(Guid? documentTypeId, string? notes, string? updatedByUserId, string? updatedByName)
    {
        DocumentTypeId = documentTypeId;
        Notes = string.IsNullOrWhiteSpace(notes) ? null : notes.Trim();
        UpdatedByUserId = updatedByUserId;
        UpdatedByName = updatedByName;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void Delete(string? deletedBy)
    {
        IsDeleted = true;
        DeletedOnUtc = DateTimeOffset.UtcNow;
        DeletedBy = deletedBy;
    }
}
