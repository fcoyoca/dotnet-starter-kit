using FSH.Modules.Administration.Contracts.Dtos;
using FSH.Modules.Patient.Contracts.Dtos;

namespace FSH.Modules.Patient.Services;

/// <summary>Patient banner shown on every exported report page (legacy BackChart template:
/// Patient / DOB / DOIV / DOL / DX).</summary>
public sealed record ReportPdfPatientInfo(
    string FullName,
    string PatientCode,
    DateTime? DateOfBirth,
    DateTime? DateOfInitialVisit,
    DateTime? DateOfLoss,
    string? DiagnosisCodes);

/// <summary>One narrative section of a report: the field definition's name/category plus the
/// captured text, already ordered for display.</summary>
public sealed record ReportPdfSection(string Name, string? Category, string Text);

public sealed record ReportPdfAddendum(string? CreatedByName, DateTime CreatedAtUtc, string Text);

/// <summary>Render model for a single report — lookups (report type name, field names) are resolved
/// by the caller so the renderer stays a pure model → bytes function.
/// <see cref="SupportsVitals"/> is whether the report type's template includes the Clinical Exam
/// category (legacy rcID 8): only those types (Initial Evaluation / Progress / Discharge) print
/// vitals — Daily Visit and No Show never do, even if values were captured.
/// <see cref="IsSigned"/> drives the DRAFT watermark: an unsigned report can still be printed, but
/// it must not be mistakable for a finalised clinical record.</summary>
public sealed record ReportPdfModel(
    string ReportTypeName,
    DateTime ReportDate,
    int Version,
    bool IsNoShow,
    string WorkflowStatus,
    bool IsSigned,
    ReportVitalsDto Vitals,
    bool SupportsVitals,
    IReadOnlyList<ReportPdfSection> Sections,
    string? SignedByName,
    DateTime? SignedOnUtc,
    string? ReviewSignedByName,
    DateTime? ReviewSignedOnUtc,
    IReadOnlyList<ReportPdfAddendum> Addendums,
    string? ClinicName,
    PrintOrientation Orientation,
    string? DepartmentName,
    string? ProviderName,
    DateTime? ModifiedOnUtc,
    byte[]? SignatureImage,
    byte[]? ReviewSignatureImage);

public interface IPatientReportPdfRenderer
{
    /// <summary>Renders the reports as one PDF document; each report starts on a new page.</summary>
    byte[] Render(ReportPdfPatientInfo patient, IReadOnlyList<ReportPdfModel> reports);
}
