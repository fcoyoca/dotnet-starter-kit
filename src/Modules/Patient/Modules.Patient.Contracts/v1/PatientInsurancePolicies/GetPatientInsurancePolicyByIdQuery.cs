using FSH.Modules.Patient.Contracts.Dtos;
using Mediator;

namespace FSH.Modules.Patient.Contracts.v1.PatientInsurancePolicies;

public sealed record GetPatientInsurancePolicyByIdQuery(Guid PolicyId) : IQuery<PatientInsurancePolicyDto>;
