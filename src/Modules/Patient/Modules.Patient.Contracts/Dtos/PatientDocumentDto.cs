namespace FSH.Modules.Patient.Contracts.Dtos;

public sealed record PatientDocumentDto(
    Guid Id,
    Guid PatientId,
    Guid? DocumentTypeId,
    string FileName,
    string ContentType,
    long FileSizeBytes,
    string? Notes,
    string? UploadedByName,
    DateTime UploadedAtUtc,
    DateTime? UpdatedAtUtc);

/// <summary>A document's raw bytes for a permission-gated download (PHI is never served statically).</summary>
public sealed record PatientDocumentFileDto(byte[] Content, string FileName, string ContentType);
