using FSH.Framework.Shared.Persistence;
using FSH.Modules.Patient.Contracts.Dtos;
using Mediator;

namespace FSH.Modules.Patient.Contracts.v1.PatientDocuments;

public sealed record SearchPatientDocumentsQuery(
    Guid PatientId,
    Guid? DocumentTypeId = null,
    bool IncludeDeleted = false,
    int PageNumber = 1,
    int PageSize = 100) : IQuery<PagedResponse<PatientDocumentDto>>;
