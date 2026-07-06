namespace FSH.Modules.Patient.Contracts.Dtos;

public sealed record SuperBillProcedureDto(
    Guid Id,
    Guid ProcedureCodeId,
    string Code,
    string? Description,
    decimal Charge,
    int DisplayOrder,
    IReadOnlyList<Guid> DiagnosticIds);

/// <summary>The procedures performed (super bill) of one report. <see cref="Id"/> is null when the
/// report has no super bill yet — the GET endpoint returns an empty shell instead of 404 so the
/// dialog can open on a fresh report (legacy <c>SuperBillProcedures_Get_XML</c> behavior).</summary>
public sealed record SuperBillDto(
    Guid? Id,
    Guid ReportId,
    bool IsBilled,
    DateTime? BilledDateUtc,
    IReadOnlyList<SuperBillProcedureDto> Procedures);
