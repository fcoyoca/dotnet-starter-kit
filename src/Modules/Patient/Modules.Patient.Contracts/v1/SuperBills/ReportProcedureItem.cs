namespace FSH.Modules.Patient.Contracts.v1.SuperBills;

/// <summary>
/// One procedure line in a report's super bill: the procedure code (bare Administration id plus a
/// code/description snapshot taken at save time — the <c>PatientProblem.DiagnosticCode</c> pattern),
/// the charge, and the incident diagnostics that justify it (legacy <c>SuperBillProcedures</c> row group).
/// </summary>
public sealed record ReportProcedureItem(
    Guid ProcedureCodeId,
    string Code,
    string? Description,
    decimal Charge,
    IReadOnlyList<Guid> DiagnosticIds);
