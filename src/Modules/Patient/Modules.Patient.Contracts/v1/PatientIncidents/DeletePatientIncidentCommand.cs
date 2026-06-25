using Mediator;

namespace FSH.Modules.Patient.Contracts.v1.PatientIncidents;

public sealed record DeletePatientIncidentCommand(Guid IncidentId) : ICommand;
