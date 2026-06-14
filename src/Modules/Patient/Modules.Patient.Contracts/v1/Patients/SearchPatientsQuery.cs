using FSH.Framework.Shared.Persistence;
using FSH.Modules.Patient.Contracts.Dtos;
using Mediator;

namespace FSH.Modules.Patient.Contracts.v1.Patients;

public sealed record SearchPatientsQuery(
    string? Search = null,
    string? SsnHash = null,
    bool? IsActive = null,
    int PageNumber = 1,
    int PageSize = 20,
    string? SortBy = null,
    string? SortDir = null) : IQuery<PagedResponse<PatientListItemDto>>;
