using Mediator;

namespace FSH.Modules.Patient.Contracts.v1.SuperBills;

/// <summary>Replaces the whole procedures-performed set of a report's super bill, creating the
/// super bill on first save (legacy <c>SuperBills_Insert</c> + <c>SuperBillProcedures_Set_Multi</c>).
/// An empty list is valid and clears the set.</summary>
public sealed record SetReportProceduresCommand(
    Guid ReportId,
    IReadOnlyList<ReportProcedureItem> Procedures) : ICommand<Unit>;
