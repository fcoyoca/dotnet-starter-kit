using FSH.Modules.Patient.Contracts.Dtos;
using Mediator;

namespace FSH.Modules.Patient.Contracts.v1.PatientReports;

public sealed record UpdatePatientReportCommand(
    Guid ReportId,
    DateTime ReportDate,
    Guid? ProviderId,
    Guid? ClinicId,
    bool IsNoShow,
    ReportVitalsDto Vitals,
    IReadOnlyList<ReportFieldValueDto> FieldValues) : ICommand<Unit>;
