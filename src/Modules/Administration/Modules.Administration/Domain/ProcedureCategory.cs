using FSH.Framework.Core.Domain;

namespace FSH.Modules.Administration.Domain;

/// <summary>
/// A grouping of procedure (CPT) codes (legacy <c>LupProcedureCategories</c> — <c>lpcName</c>/<c>lpcDescription</c>/
/// <c>lpcImaging</c>/<c>lpcDeleted</c>). Tenant-scoped — not <see cref="IGlobalEntity"/>, so <c>BaseDbContext</c>
/// applies the per-tenant filter.
/// </summary>
public sealed class ProcedureCategory : AggregateRoot<Guid>, ISoftDeletable
{
    public string Name { get; private set; } = default!;
    public string? Description { get; private set; }

    /// <summary>Whether this category covers imaging procedures (legacy <c>lpcImaging</c>).</summary>
    public bool IsImaging { get; private set; }

    public bool IsActive { get; private set; }

    /// <summary>Legacy <c>lpcID</c> of the source record; null for native records.</summary>
    public int? LegacyId { get; private set; }

    public DateTime CreatedAtUtc { get; private set; }
    public DateTime? UpdatedAtUtc { get; private set; }

    public bool IsDeleted { get; private set; }
    public DateTimeOffset? DeletedOnUtc { get; private set; }
    public string? DeletedBy { get; private set; }

    private ProcedureCategory() { }

    public static ProcedureCategory Create(
        string name,
        string? description,
        bool isImaging,
        int? legacyId = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        return new ProcedureCategory
        {
            Id = Guid.CreateVersion7(),
            Name = name.Trim(),
            Description = Clean(description),
            IsImaging = isImaging,
            IsActive = true,
            LegacyId = legacyId,
            CreatedAtUtc = DateTime.UtcNow
        };
    }

    public void Update(string name, string? description, bool isImaging, bool isActive)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        Name = name.Trim();
        Description = Clean(description);
        IsImaging = isImaging;
        IsActive = isActive;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void Delete(string? deletedBy)
    {
        IsDeleted = true;
        DeletedOnUtc = DateTimeOffset.UtcNow;
        DeletedBy = deletedBy;
    }

    private static string? Clean(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
