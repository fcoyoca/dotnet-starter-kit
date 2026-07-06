using FSH.Modules.Patient.Contracts.Dtos;
using Mediator;

namespace FSH.Modules.Patient.Contracts.v1.PatientDocuments;

public sealed record DownloadPatientDocumentQuery(Guid DocumentId) : IQuery<PatientDocumentFileDto>;
