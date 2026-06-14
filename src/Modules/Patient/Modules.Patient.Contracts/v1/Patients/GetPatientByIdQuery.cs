using FSH.Modules.Patient.Contracts.Dtos;
using Mediator;

namespace FSH.Modules.Patient.Contracts.v1.Patients;

public sealed record GetPatientByIdQuery(Guid PatientId) : IQuery<PatientDetailDto>;
