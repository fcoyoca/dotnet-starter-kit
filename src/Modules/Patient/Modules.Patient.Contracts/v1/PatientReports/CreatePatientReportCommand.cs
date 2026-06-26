using Mediator;

namespace FSH.Modules.Patient.Contracts.v1.PatientReports;

public sealed record CreatePatientReportCommand(
    Guid IncidentId,
    Guid PatientId,
    int ReportTypeId,
    DateTime ReportDate,
    Guid? ProviderId,
    Guid? ClinicId,
    bool IsNoShow) : ICommand<Guid>;
