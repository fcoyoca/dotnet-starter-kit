using FSH.Modules.Administration.Contracts.Dtos;
using Mediator;

namespace FSH.Modules.Administration.Contracts.v1.PatientDocumentTypes;

public sealed record GetPatientDocumentTypeByIdQuery(Guid Id) : IQuery<PatientDocumentTypeDto>;
