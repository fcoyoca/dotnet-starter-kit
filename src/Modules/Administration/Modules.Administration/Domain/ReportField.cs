using FSH.Framework.Core.Domain;

namespace FSH.Modules.Administration.Domain;

/// <summary>
/// A field within a clinical report template (legacy <c>ReportFields</c> — <c>rfID</c>/<c>rfName</c>/
/// <c>rfReportCategoryID</c>). Fields are grouped by <see cref="Category"/> and shared across report types
/// (a field that appears in several types shares its macros). Cross-tenant system catalog
/// (<see cref="IGlobalEntity"/>); seeded, read-only reference data. Macros associate to a field via
/// <c>Macro.ReportFieldId</c>.
/// </summary>
public sealed class ReportField : AggregateRoot<int>, IGlobalEntity
{
    public string Name { get; init; } = default!;

    /// <summary>The report category the field belongs to (denormalized legacy <c>rcName</c>).</summary>
    public string? Category { get; init; }

    public int DisplayOrder { get; init; }
    public bool IsActive { get; init; }
}
