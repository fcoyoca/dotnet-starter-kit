using FSH.Framework.Core.Domain;

namespace FSH.Modules.Administration.Domain;

/// <summary>
/// A reusable named snippet of report/note text (legacy <c>ReportFieldMacros</c> — <c>rfmName</c>/<c>rfmData</c>/
/// <c>rfmActive</c>/<c>rfmReportFieldID</c>). Associated to a <see cref="ReportField"/> via
/// <see cref="ReportFieldId"/> (null = "All (General)", legacy <c>rfmReportFieldID</c> NULL/-101). The legacy
/// per-user owner (<c>rfmUserID</c>) is not migrated — macros are tenant-wide here.
/// Tenant-scoped — not <see cref="IGlobalEntity"/>, so <c>BaseDbContext</c> applies the per-tenant filter.
/// </summary>
public sealed class Macro : AggregateRoot<Guid>, ISoftDeletable
{
    public string Name { get; private set; } = default!;

    /// <summary>The macro body — the text inserted when the macro is applied.</summary>
    public string? Text { get; private set; }

    /// <summary>The report field this macro is for; null = "All (General)".</summary>
    public int? ReportFieldId { get; private set; }

    public bool IsActive { get; private set; }

    /// <summary>Legacy <c>rfmID</c> of the source record; null for native records.</summary>
    public int? LegacyId { get; private set; }

    public DateTime CreatedAtUtc { get; private set; }
    public DateTime? UpdatedAtUtc { get; private set; }

    public bool IsDeleted { get; private set; }
    public DateTimeOffset? DeletedOnUtc { get; private set; }
    public string? DeletedBy { get; private set; }

    private Macro() { }

    public static Macro Create(string name, string? text, int? reportFieldId, int? legacyId = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        return new Macro
        {
            Id = Guid.CreateVersion7(),
            Name = name.Trim(),
            Text = string.IsNullOrWhiteSpace(text) ? null : text,
            ReportFieldId = reportFieldId,
            IsActive = true,
            LegacyId = legacyId,
            CreatedAtUtc = DateTime.UtcNow
        };
    }

    public void Update(string name, string? text, int? reportFieldId, bool isActive)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        Name = name.Trim();
        Text = string.IsNullOrWhiteSpace(text) ? null : text;
        ReportFieldId = reportFieldId;
        IsActive = isActive;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void Delete(string? deletedBy)
    {
        IsDeleted = true;
        DeletedOnUtc = DateTimeOffset.UtcNow;
        DeletedBy = deletedBy;
    }
}
