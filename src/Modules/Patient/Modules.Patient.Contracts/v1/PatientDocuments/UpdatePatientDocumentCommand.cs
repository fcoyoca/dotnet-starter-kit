using Mediator;

namespace FSH.Modules.Patient.Contracts.v1.PatientDocuments;

/// <summary>Updates a document's metadata (type + notes). The file itself is immutable —
/// re-upload to replace, matching legacy behaviour.</summary>
public sealed record UpdatePatientDocumentCommand(
    Guid DocumentId,
    Guid? DocumentTypeId,
    string? Notes) : ICommand<Unit>;
