using FSH.Modules.Patient.Contracts.Dtos;
using Mediator;

namespace FSH.Modules.Patient.Contracts.v1.PatientIncidents;

public sealed record GetPatientIncidentByIdQuery(Guid IncidentId) : IQuery<PatientIncidentDetailDto>;
