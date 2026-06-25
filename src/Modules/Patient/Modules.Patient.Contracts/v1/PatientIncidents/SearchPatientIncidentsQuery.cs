using FSH.Framework.Shared.Persistence;
using FSH.Modules.Patient.Contracts.Dtos;
using Mediator;

namespace FSH.Modules.Patient.Contracts.v1.PatientIncidents;

public sealed record SearchPatientIncidentsQuery(
    Guid PatientId,
    bool? IsClosed = null,
    bool IncludeDeleted = false,
    int PageNumber = 1,
    int PageSize = 50) : IQuery<PagedResponse<PatientIncidentListItemDto>>;
