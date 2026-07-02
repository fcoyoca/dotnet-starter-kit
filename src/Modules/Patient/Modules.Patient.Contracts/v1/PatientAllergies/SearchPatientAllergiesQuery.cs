using FSH.Framework.Shared.Persistence;
using FSH.Modules.Patient.Contracts.Dtos;
using Mediator;

namespace FSH.Modules.Patient.Contracts.v1.PatientAllergies;

public sealed record SearchPatientAllergiesQuery(
    Guid PatientId,
    bool IncludeInactive = false,
    int PageNumber = 1,
    int PageSize = 100) : IQuery<PagedResponse<PatientAllergyDto>>;
