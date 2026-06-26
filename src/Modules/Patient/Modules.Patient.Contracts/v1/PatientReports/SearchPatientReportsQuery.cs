using FSH.Framework.Shared.Persistence;
using FSH.Modules.Patient.Contracts.Dtos;
using Mediator;

namespace FSH.Modules.Patient.Contracts.v1.PatientReports;

public sealed record SearchPatientReportsQuery(
    Guid? IncidentId = null,
    Guid? PatientId = null,
    string? Search = null,
    bool IncludeDeleted = false,
    int PageNumber = 1,
    int PageSize = 50) : IQuery<PagedResponse<PatientReportListItemDto>>;
