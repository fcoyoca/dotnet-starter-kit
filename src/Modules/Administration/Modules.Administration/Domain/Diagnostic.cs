using FSH.Framework.Core.Domain;

namespace FSH.Modules.Administration.Domain;

/// <summary>
/// A diagnosis code in the global ICD catalog ("Diagnostic Details" — legacy <c>LupDiagnostics</c>
/// (<c>ldxCode</c>/<c>ldxDescription</c>/<c>ldxLongDescription</c>/<c>ldxDiagnosticSource</c>) and the
/// CMS <c>ICD10CMCodes</c> reference set). Cross-tenant reference data (<see cref="IGlobalEntity"/>) like the
/// other Administration lookups; the <see cref="CodeSourceId"/> is the "ICD 10/9" source dropdown.
/// Soft-deleted so a code in use can be hidden without breaking historic references.
/// </summary>
public sealed class Diagnostic : AggregateRoot<int>, ISoftDeletable, IGlobalEntity
{
    public string Code { get; private set; } = default!;
    public string? Description { get; private set; }
    public string? LongDescription { get; private set; }

    /// <summary>The code system this diagnosis belongs to (→ <see cref="CodeSource"/>); the "ICD 10" dropdown.</summary>
    public int CodeSourceId { get; private set; }

    public bool IsChiropractic { get; private set; }
    /// <summary>ICD-10-CM billable vs header (non-billable category) distinction; null when unknown.</summary>
    public bool? IsBillable { get; private set; }
    public bool IsActive { get; private set; }

    /// <summary>Legacy <c>ldxID</c> of the source record; null for native records.</summary>
    public int? LegacyId { get; private set; }

    public DateTime CreatedAtUtc { get; private set; }
    public DateTime? UpdatedAtUtc { get; private set; }

    public bool IsDeleted { get; private set; }
    public DateTimeOffset? DeletedOnUtc { get; private set; }
    public string? DeletedBy { get; private set; }

    private Diagnostic() { }

    public static Diagnostic Create(
        string code,
        string? description,
        string? longDescription,
        int codeSourceId,
        bool isChiropractic = false,
        bool? isBillable = null,
        int? legacyId = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(code);
        return new Diagnostic
        {
            Code = code.Trim(),
            Description = Clean(description),
            LongDescription = Clean(longDescription),
            CodeSourceId = codeSourceId,
            IsChiropractic = isChiropractic,
            IsBillable = isBillable,
            IsActive = true,
            LegacyId = legacyId,
            CreatedAtUtc = DateTime.UtcNow
        };
    }

    public void Update(
        string code,
        string? description,
        string? longDescription,
        int codeSourceId,
        bool isChiropractic,
        bool? isBillable,
        bool isActive)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(code);
        Code = code.Trim();
        Description = Clean(description);
        LongDescription = Clean(longDescription);
        CodeSourceId = codeSourceId;
        IsChiropractic = isChiropractic;
        IsBillable = isBillable;
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
