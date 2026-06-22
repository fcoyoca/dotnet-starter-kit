using Mediator;

namespace FSH.Modules.Administration.Contracts.v1.PatientDocumentTypes;

public sealed record DeletePatientDocumentTypeCommand(Guid Id) : ICommand<Unit>;
