using FSH.Modules.Patient.Contracts.Dtos;
using Mediator;

namespace FSH.Modules.Patient.Contracts.v1.PatientReports;

/// <summary>Renders the given reports (all belonging to one patient, typically one incident) as a
/// single PDF document — multiple reports are merged in report-date order, one report per page run.
/// Clients wanting separate files call once per report.</summary>
public sealed record ExportPatientReportsPdfQuery(IReadOnlyList<Guid> ReportIds) : IQuery<ExportedReportsPdfDto>;
