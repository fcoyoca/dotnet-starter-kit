using FSH.Modules.Patient.Contracts.Dtos;

namespace FSH.Modules.Patient.Services;

/// <summary>Patient banner shown on every exported report page.</summary>
public sealed record ReportPdfPatientInfo(
    string FullName,
    string PatientCode,
    DateTime? DateOfBirth,
    string? Gender);

/// <summary>One narrative section of a report: the field definition's name/category plus the
/// captured text, already ordered for display.</summary>
public sealed record ReportPdfSection(string Name, string? Category, string Text);

public sealed record ReportPdfAddendum(string? CreatedByName, DateTime CreatedAtUtc, string Text);

/// <summary>Render model for a single report — lookups (report type name, field names) are resolved
/// by the caller so the renderer stays a pure model → bytes function.
/// <see cref="SupportsVitals"/> is whether the report type's template includes the Clinical Exam
/// category (legacy rcID 8): only those types (Initial Evaluation / Progress / Discharge) print
/// vitals — Daily Visit and No Show never do, even if values were captured.</summary>
public sealed record ReportPdfModel(
    string ReportTypeName,
    DateTime ReportDate,
    int Version,
    bool IsNoShow,
    string WorkflowStatus,
    ReportVitalsDto Vitals,
    bool SupportsVitals,
    IReadOnlyList<ReportPdfSection> Sections,
    string? SignedByName,
    DateTime? SignedOnUtc,
    string? ReviewSignedByName,
    DateTime? ReviewSignedOnUtc,
    IReadOnlyList<ReportPdfAddendum> Addendums);

public interface IPatientReportPdfRenderer
{
    /// <summary>Renders the reports as one PDF document; each report starts on a new page.</summary>
    byte[] Render(ReportPdfPatientInfo patient, IReadOnlyList<ReportPdfModel> reports);
}
