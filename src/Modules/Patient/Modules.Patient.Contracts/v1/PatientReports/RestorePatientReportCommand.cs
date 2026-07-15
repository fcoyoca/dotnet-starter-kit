using Mediator;

namespace FSH.Modules.Patient.Contracts.v1.PatientReports;

public sealed record RestorePatientReportCommand(Guid ReportId) : ICommand<Unit>;
