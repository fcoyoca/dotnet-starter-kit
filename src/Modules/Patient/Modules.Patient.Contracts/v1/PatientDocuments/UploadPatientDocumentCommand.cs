using Mediator;

namespace FSH.Modules.Patient.Contracts.v1.PatientDocuments;

/// <summary>Uploads one document for a patient. <paramref name="ContentBase64"/> carries the file
/// bytes (same JSON+base64 transport as provider signatures); clients upload multiple files with one
/// call per file. Returns the new document id.</summary>
public sealed record UploadPatientDocumentCommand(
    Guid PatientId,
    Guid? DocumentTypeId,
    string FileName,
    string ContentBase64,
    string? ContentType,
    string? Notes) : ICommand<Guid>;
