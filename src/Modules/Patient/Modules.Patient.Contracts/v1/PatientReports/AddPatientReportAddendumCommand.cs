using Mediator;

namespace FSH.Modules.Patient.Contracts.v1.PatientReports;

public sealed record AddPatientReportAddendumCommand(Guid ReportId, string Text) : ICommand<Guid>;
