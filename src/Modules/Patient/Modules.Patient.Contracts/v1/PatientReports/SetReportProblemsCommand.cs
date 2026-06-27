using Mediator;

namespace FSH.Modules.Patient.Contracts.v1.PatientReports;

/// <summary>Replace the set of patient problems associated with a report (legacy "Associated Problems").</summary>
public sealed record SetReportProblemsCommand(Guid ReportId, IReadOnlyList<Guid> ProblemIds) : ICommand<Unit>;
