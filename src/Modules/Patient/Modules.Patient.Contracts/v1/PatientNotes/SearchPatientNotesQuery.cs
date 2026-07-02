using FSH.Framework.Shared.Persistence;
using FSH.Modules.Patient.Contracts.Dtos;
using Mediator;

namespace FSH.Modules.Patient.Contracts.v1.PatientNotes;

public sealed record SearchPatientNotesQuery(
    Guid PatientId,
    bool MedicalAlertsOnly = false,
    bool IncludeDeleted = false,
    int PageNumber = 1,
    int PageSize = 100) : IQuery<PagedResponse<PatientNoteDto>>;
