using FSH.Modules.Patient.Contracts.Dtos;
using Mediator;

namespace FSH.Modules.Patient.Contracts.v1.PatientMedications;

public sealed record GetPatientMedicationByIdQuery(Guid MedicationId) : IQuery<PatientMedicationDto>;
