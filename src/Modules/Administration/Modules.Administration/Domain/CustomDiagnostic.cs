using FSH.Framework.Core.Domain;

namespace FSH.Modules.Administration.Domain;

/// <summary>
/// A tenant-defined custom diagnostic code (legacy <c>CustomDiagnostics</c> — <c>cdxCode</c>/<c>cdxDescription</c>/
/// <c>cdxLongDescription</c>/<c>cdxIsChiropractic</c>). Tenant-scoped — not <see cref="IGlobalEntity"/>, so
/// <c>BaseDbContext</c> applies the per-tenant filter.
/// </summary>
public sealed class CustomDiagnostic : AggregateRoot<Guid>, ISoftDeletable
{
    public string Code { get; private set; } = default!;
    public string? Description { get; private set; }
    public string? LongDescription { get; private set; }
    public bool IsChiropractic { get; private set; }
    public bool IsActive { get; private set; }

    /// <summary>Legacy <c>cdxID</c> of the source record; null for native records.</summary>
    public int? LegacyId { get; private set; }

    public DateTime CreatedAtUtc { get; private set; }
    public DateTime? UpdatedAtUtc { get; private set; }

    public bool IsDeleted { get; private set; }
    public DateTimeOffset? DeletedOnUtc { get; private set; }
    public string? DeletedBy { get; private set; }

    private CustomDiagnostic() { }

    public static CustomDiagnostic Create(
        string code,
        string? description,
        string? longDescription,
        bool isChiropractic,
        int? legacyId = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(code);

        return new CustomDiagnostic
        {
            Id = Guid.CreateVersion7(),
            Code = code.Trim(),
            Description = Clean(description),
            LongDescription = Clean(longDescription),
            IsChiropractic = isChiropractic,
            IsActive = true,
            LegacyId = legacyId,
            CreatedAtUtc = DateTime.UtcNow
        };
    }

    public void Update(
        string code,
        string? description,
        string? longDescription,
        bool isChiropractic,
        bool isActive)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(code);

        Code = code.Trim();
        Description = Clean(description);
        LongDescription = Clean(longDescription);
        IsChiropractic = isChiropractic;
        IsActive = isActive;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void Delete(string? deletedBy)
    {
        IsDeleted = true;
        DeletedOnUtc = DateTimeOffset.UtcNow;
        DeletedBy = deletedBy;
    }

    /// <summary>
    /// Undoes a soft delete and re-marks the record active. Used by the "ensure" find-or-create flow when a
    /// previously deleted/inactive record matches a newly-picked global code.
    /// </summary>
    public void Reactivate()
    {
        IsDeleted = false;
        DeletedOnUtc = null;
        DeletedBy = null;
        IsActive = true;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    private static string? Clean(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
