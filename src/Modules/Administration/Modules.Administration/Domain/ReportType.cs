using FSH.Framework.Core.Domain;

namespace FSH.Modules.Administration.Domain;

/// <summary>
/// A clinical report template type (legacy <c>ReportTypes</c> — <c>rtID</c>/<c>rtName</c>), e.g. Initial Evaluation,
/// Progress Report. Tenant-scoped — each tenant manages its own set (seeded with the standard catalog on first run).
/// Soft-deleted so a type in use can be hidden without breaking historic references.
/// </summary>
public sealed class ReportType : AggregateRoot<int>, ISoftDeletable
{
    public string Name { get; private set; } = default!;
    public int DisplayOrder { get; private set; }
    public bool IsActive { get; private set; }

    /// <summary>Legacy <c>rtID</c> of the source record; null for native records.</summary>
    public int? LegacyId { get; private set; }

    public bool IsDeleted { get; private set; }
    public DateTimeOffset? DeletedOnUtc { get; private set; }
    public string? DeletedBy { get; private set; }

    private ReportType() { }

    public static ReportType Create(string name, int displayOrder = 0, int? legacyId = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        return new ReportType
        {
            Name = name.Trim(),
            DisplayOrder = displayOrder,
            IsActive = true,
            LegacyId = legacyId,
        };
    }

    public void Update(string name, int displayOrder, bool isActive)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        Name = name.Trim();
        DisplayOrder = displayOrder;
        IsActive = isActive;
    }

    public void Delete(string? deletedBy)
    {
        IsDeleted = true;
        DeletedOnUtc = DateTimeOffset.UtcNow;
        DeletedBy = deletedBy;
    }
}
