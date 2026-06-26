using Mediator;

namespace FSH.Modules.Patient.Contracts.v1.PatientReports;

public sealed record ReviewSignReportCommand(Guid ReportId) : ICommand<Unit>;
