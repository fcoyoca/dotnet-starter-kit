using FSH.Modules.Patient.Contracts.Dtos;
using Mediator;

namespace FSH.Modules.Patient.Contracts.v1.PatientReports;

/// <summary>Renders the given reports (all belonging to one patient, typically one incident) as a
/// single PDF document — multiple reports are merged in report-date order, one report per page run.
/// Clients wanting separate files call once per report.
/// <para><c>Password</c> is the legacy "padlock" export: when supplied, the PDF is AES-256 encrypted
/// and opens only with that password. The password is never persisted — the caller is responsible
/// for delivering it to the recipient out of band.</para></summary>
public sealed record ExportPatientReportsPdfQuery(
    IReadOnlyList<Guid> ReportIds,
    string? Password = null) : IQuery<ExportedReportsPdfDto>;
