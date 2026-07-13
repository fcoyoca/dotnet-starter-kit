using Mediator;

namespace FSH.Modules.Patient.Contracts.v1.PatientInsurancePolicies;

public sealed record DeletePatientInsurancePolicyCommand(Guid PolicyId) : ICommand<Unit>;
