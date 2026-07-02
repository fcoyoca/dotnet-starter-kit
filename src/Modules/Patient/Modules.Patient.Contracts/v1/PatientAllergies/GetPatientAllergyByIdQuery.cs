using FSH.Modules.Patient.Contracts.Dtos;
using Mediator;

namespace FSH.Modules.Patient.Contracts.v1.PatientAllergies;

public sealed record GetPatientAllergyByIdQuery(Guid AllergyId) : IQuery<PatientAllergyDto>;
