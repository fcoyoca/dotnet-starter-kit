using FSH.Modules.Patient.Contracts.Dtos;
using Mediator;

namespace FSH.Modules.Patient.Contracts.v1.PatientReports;

public sealed record GetPatientReportByIdQuery(Guid ReportId) : IQuery<PatientReportDetailDto>;
