using FSH.Framework.Shared.Persistence;
using FSH.Modules.Patient.Contracts.Dtos;
using Mediator;

namespace FSH.Modules.Patient.Contracts.v1.PatientMedications;

public sealed record SearchPatientMedicationsQuery(
    Guid PatientId,
    bool IncludeInactive = false,
    int PageNumber = 1,
    int PageSize = 100) : IQuery<PagedResponse<PatientMedicationDto>>;
