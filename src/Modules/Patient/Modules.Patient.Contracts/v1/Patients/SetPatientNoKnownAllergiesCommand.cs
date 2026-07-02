using Mediator;

namespace FSH.Modules.Patient.Contracts.v1.Patients;

public sealed record SetPatientNoKnownAllergiesCommand(Guid PatientId, bool Value) : ICommand<Unit>;
