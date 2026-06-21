using FSH.Framework.Core.Domain;

namespace FSH.Modules.Administration.Domain;

/// <summary>
/// A clinical provider (doctor / rendering provider) within a tenant. In legacy BackChart a
/// "doctor" was just a user with <c>uDoctor=1</c>; here a Provider is its own tenant-scoped record so
/// non-login providers can exist and billing/scheduling can reference a stable identity. Optionally
/// links to an Identity user (<see cref="UserId"/>) and to a primary <see cref="Clinic"/>.
/// </summary>
public sealed class Provider : AggregateRoot<Guid>, ISoftDeletable
{
    public string FirstName { get; private set; } = default!;
    public string LastName { get; private set; } = default!;
    public string? Prefix { get; private set; }
    public string? Suffix { get; private set; }
    public string? Specialty { get; private set; }

    /// <summary>National Provider Identifier (10 digits). Optional; unique per tenant when present.</summary>
    public string? Npi { get; private set; }

    /// <summary>Kareo/Tebra provider id (legacy <c>uExternalId</c>) used for billing sync.</summary>
    public string? KareoExternalId { get; private set; }

    /// <summary>Optional FK to a <see cref="Clinic"/> in this module (same DbContext).</summary>
    public Guid? PrimaryClinicId { get; private set; }

    /// <summary>
    /// Optional link to an Identity <c>FshUser</c> (string key). Bare reference — Identity is a
    /// separate module/DbContext, so no EF FK constraint. Null for non-login providers.
    /// </summary>
    public string? UserId { get; private set; }

    public bool IsActive { get; private set; }

    /// <summary>Legacy <c>uID</c> of the source user that represented this provider; null for native records.</summary>
    public int? LegacyUserId { get; private set; }

    public DateTime CreatedAtUtc { get; private set; }
    public DateTime? UpdatedAtUtc { get; private set; }

    public bool IsDeleted { get; private set; }
    public DateTimeOffset? DeletedOnUtc { get; private set; }
    public string? DeletedBy { get; private set; }

    private Provider() { }

    public static Provider Create(
        string firstName,
        string lastName,
        string? prefix,
        string? suffix,
        string? specialty,
        string? npi,
        string? kareoExternalId,
        Guid? primaryClinicId,
        string? userId,
        int? legacyUserId = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(firstName);
        ArgumentException.ThrowIfNullOrWhiteSpace(lastName);

        return new Provider
        {
            Id = Guid.CreateVersion7(),
            FirstName = firstName.Trim(),
            LastName = lastName.Trim(),
            Prefix = Clean(prefix),
            Suffix = Clean(suffix),
            Specialty = Clean(specialty),
            Npi = Clean(npi),
            KareoExternalId = Clean(kareoExternalId),
            PrimaryClinicId = primaryClinicId,
            UserId = Clean(userId),
            IsActive = true,
            LegacyUserId = legacyUserId,
            CreatedAtUtc = DateTime.UtcNow
        };
    }

    public void Update(
        string firstName,
        string lastName,
        string? prefix,
        string? suffix,
        string? specialty,
        string? npi,
        string? kareoExternalId,
        Guid? primaryClinicId,
        string? userId,
        bool isActive)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(firstName);
        ArgumentException.ThrowIfNullOrWhiteSpace(lastName);

        FirstName = firstName.Trim();
        LastName = lastName.Trim();
        Prefix = Clean(prefix);
        Suffix = Clean(suffix);
        Specialty = Clean(specialty);
        Npi = Clean(npi);
        KareoExternalId = Clean(kareoExternalId);
        PrimaryClinicId = primaryClinicId;
        UserId = Clean(userId);
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
