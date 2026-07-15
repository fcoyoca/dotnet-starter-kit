using Mediator;

namespace FSH.Modules.Patient.Contracts.v1.PatientIncidents;

public sealed record RestorePatientIncidentCommand(Guid IncidentId) : ICommand;
