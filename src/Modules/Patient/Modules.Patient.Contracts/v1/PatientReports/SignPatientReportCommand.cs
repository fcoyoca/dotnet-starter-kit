using Mediator;

namespace FSH.Modules.Patient.Contracts.v1.PatientReports;

/// <summary>Sign a draft report. The signer is the current user; the signature image is snapshotted
/// from the report's provider (optional — null when the provider has none).</summary>
public sealed record SignPatientReportCommand(Guid ReportId) : ICommand<Unit>;
