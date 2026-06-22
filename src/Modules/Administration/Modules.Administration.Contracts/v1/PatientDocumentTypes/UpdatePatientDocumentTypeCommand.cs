using Mediator;

namespace FSH.Modules.Administration.Contracts.v1.PatientDocumentTypes;

public sealed record UpdatePatientDocumentTypeCommand(
    Guid Id,
    string Name,
    bool IsActive) : ICommand<Unit>;
