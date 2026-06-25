using Mediator;

namespace FSH.Modules.Patient.Contracts.v1.PatientIncidents;

public sealed record ClosePatientIncidentCommand(Guid IncidentId) : ICommand;
