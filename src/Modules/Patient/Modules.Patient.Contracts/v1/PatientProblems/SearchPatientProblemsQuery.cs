using FSH.Framework.Shared.Persistence;
using FSH.Modules.Patient.Contracts.Dtos;
using Mediator;

namespace FSH.Modules.Patient.Contracts.v1.PatientProblems;

public sealed record SearchPatientProblemsQuery(
    Guid PatientId,
    bool IncludeInactive = false,
    bool IncludeResolved = false,
    bool IncludeDeleted = false,
    bool MedicalAlertsOnly = false,
    int PageNumber = 1,
    int PageSize = 100) : IQuery<PagedResponse<PatientProblemDto>>;
