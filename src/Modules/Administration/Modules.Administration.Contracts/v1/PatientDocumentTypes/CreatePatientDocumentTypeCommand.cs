using Mediator;

namespace FSH.Modules.Administration.Contracts.v1.PatientDocumentTypes;

public sealed record CreatePatientDocumentTypeCommand(string Name) : ICommand<Guid>;
