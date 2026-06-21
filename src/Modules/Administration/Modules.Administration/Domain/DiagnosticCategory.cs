using FSH.Framework.Core.Domain;

namespace FSH.Modules.Administration.Domain;

/// <summary>
/// A grouping of diagnostic (ICD) codes (legacy <c>LupDiagnosticCategories</c> — <c>ldcName</c>/<c>ldcDeleted</c>).
/// Tenant-scoped — not <see cref="IGlobalEntity"/>, so <c>BaseDbContext</c> applies the per-tenant filter.
/// The category↔ICD-code membership grid is deferred until the global ICD master list is migrated.
/// </summary>
public sealed class DiagnosticCategory : AggregateRoot<Guid>, ISoftDeletable
{
    public string Name { get; private set; } = default!;
    public bool IsActive { get; private set; }

    /// <summary>Legacy <c>ldcID</c> of the source record; null for native records.</summary>
    public int? LegacyId { get; private set; }

    public DateTime CreatedAtUtc { get; private set; }
    public DateTime? UpdatedAtUtc { get; private set; }

    public bool IsDeleted { get; private set; }
    public DateTimeOffset? DeletedOnUtc { get; private set; }
    public string? DeletedBy { get; private set; }

    private DiagnosticCategory() { }

    public static DiagnosticCategory Create(string name, int? legacyId = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        return new DiagnosticCategory
        {
            Id = Guid.CreateVersion7(),
            Name = name.Trim(),
            IsActive = true,
            LegacyId = legacyId,
            CreatedAtUtc = DateTime.UtcNow
        };
    }

    public void Update(string name, bool isActive)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        Name = name.Trim();
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
