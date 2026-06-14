using Mediator;

namespace FSH.Modules.Patient.Contracts.v1.Patients;

public sealed record DeletePatientCommand(Guid PatientId) : ICommand;
