using Mediator;

namespace FSH.Modules.Patient.Contracts.v1.PatientDocuments;

public sealed record DeletePatientDocumentCommand(Guid DocumentId) : ICommand<Unit>;
