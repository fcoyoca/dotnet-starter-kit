using Mediator;

namespace FSH.Modules.Patient.Contracts.v1.PatientMedications;

public sealed record MarkMedicationsReconciledCommand(Guid PatientId) : ICommand<Guid>;
