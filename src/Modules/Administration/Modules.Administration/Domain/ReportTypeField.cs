using FSH.Framework.Core.Domain;

namespace FSH.Modules.Administration.Domain;

/// <summary>
/// Maps which <see cref="ReportField"/>s belong to a <see cref="ReportType"/> (legacy resolves through
/// <c>ReportTypeCategories</c> → <c>ReportFields</c>; flattened here). A field can map to several types — that is
/// how legacy "fields that exist in more than one report type share macros" works. Cross-tenant system catalog
/// (<see cref="IGlobalEntity"/>); seeded, read-only.
/// </summary>
public sealed class ReportTypeField : AggregateRoot<int>, IGlobalEntity
{
    public int ReportTypeId { get; init; }
    public int ReportFieldId { get; init; }
}
