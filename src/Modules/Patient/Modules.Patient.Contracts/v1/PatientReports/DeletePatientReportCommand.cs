using Mediator;

namespace FSH.Modules.Patient.Contracts.v1.PatientReports;

public sealed record DeletePatientReportCommand(Guid ReportId) : ICommand<Unit>;
