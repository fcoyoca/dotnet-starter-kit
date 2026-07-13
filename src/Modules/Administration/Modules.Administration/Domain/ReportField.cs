using FSH.Framework.Core.Domain;

namespace FSH.Modules.Administration.Domain;

/// <summary>
/// A field within a clinical report template (legacy <c>ReportFields</c> — <c>rfID</c>/<c>rfName</c>). Belongs to a
/// single <see cref="ReportType"/> via <see cref="ReportTypeId"/>; grouped for display by <see cref="Category"/>.
/// Tenant-scoped CRUD (seeded with the standard catalog on first run). Macros associate to a field via
/// <c>Macro.ReportFieldId</c>. Soft-deleted so a field in use can be hidden.
/// </summary>
public sealed class ReportField : AggregateRoot<int>, ISoftDeletable
{
    public int ReportTypeId { get; private set; }
    public string Name { get; private set; } = default!;

    /// <summary>Optional grouping label shown on the report (legacy <c>rcName</c>).</summary>
    public string? Category { get; private set; }

    public int DisplayOrder { get; private set; }
    public bool IsActive { get; private set; }

    /// <summary>
    /// Boilerplate stamped into this field when a report is first created (legacy <c>rfDefaultText</c>, set on the
    /// "Report Default Text" admin screen). Applied at creation only — editing it later never rewrites existing
    /// reports, whose field text belongs to the report from that point on.
    /// </summary>
    public string? DefaultText { get; private set; }

    /// <summary>Legacy <c>rfID</c> of the source record; null for native records.</summary>
    public int? LegacyId { get; private set; }

    public bool IsDeleted { get; private set; }
    public DateTimeOffset? DeletedOnUtc { get; private set; }
    public string? DeletedBy { get; private set; }

    private ReportField() { }

    public static ReportField Create(
        int reportTypeId,
        string name,
        string? category = null,
        int displayOrder = 0,
        bool isActive = true,
        int? legacyId = null,
        string? defaultText = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        return new ReportField
        {
            ReportTypeId = reportTypeId,
            Name = name.Trim(),
            Category = string.IsNullOrWhiteSpace(category) ? null : category.Trim(),
            DisplayOrder = displayOrder,
            IsActive = isActive,
            LegacyId = legacyId,
            DefaultText = Normalize(defaultText),
        };
    }

    public void Update(string name, string? category, int displayOrder, bool isActive, string? defaultText)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        Name = name.Trim();
        Category = string.IsNullOrWhiteSpace(category) ? null : category.Trim();
        DisplayOrder = displayOrder;
        IsActive = isActive;
        DefaultText = Normalize(defaultText);
    }

    /// <summary>Blank and whitespace-only default text both mean "no boilerplate" — stored as null so the
    /// create-time prefill has a single emptiness check to make.</summary>
    private static string? Normalize(string? defaultText) =>
        string.IsNullOrWhiteSpace(defaultText) ? null : defaultText;

    public void Delete(string? deletedBy)
    {
        IsDeleted = true;
        DeletedOnUtc = DateTimeOffset.UtcNow;
        DeletedBy = deletedBy;
    }
}
