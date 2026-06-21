using FSH.Framework.Core.Domain;

namespace FSH.Modules.Administration.Domain;

/// <summary>
/// An insurance company / payer (legacy <c>InsuranceName</c> / <c>lupInsuranceTypeAndName</c>).
/// Tenant-scoped. Optionally belongs to an <see cref="InsuranceType"/> and carries a single
/// embedded billing/claims address. Multiple addresses per company are deferred to a later sprint.
/// </summary>
public sealed class InsuranceCompany : AggregateRoot<Guid>, ISoftDeletable
{
    public string Name { get; private set; } = default!;

    /// <summary>Optional FK to an <see cref="InsuranceType"/> in this module (same DbContext).</summary>
    public Guid? InsuranceTypeId { get; private set; }

    /// <summary>Number of drug-formulary tiers for this payer (legacy <c>litFormularyTiers</c>).</summary>
    public int FormularyTiers { get; private set; }

    public string? Address1 { get; private set; }
    public string? Address2 { get; private set; }
    public string? City { get; private set; }
    public string? State { get; private set; }
    public string? Zip { get; private set; }
    public string? Phone { get; private set; }

    public bool IsActive { get; private set; }

    /// <summary>Legacy <c>litID</c> of the source record; null for native records.</summary>
    public int? LegacyId { get; private set; }

    public DateTime CreatedAtUtc { get; private set; }
    public DateTime? UpdatedAtUtc { get; private set; }

    public bool IsDeleted { get; private set; }
    public DateTimeOffset? DeletedOnUtc { get; private set; }
    public string? DeletedBy { get; private set; }

    private InsuranceCompany() { }

    public static InsuranceCompany Create(
        string name,
        Guid? insuranceTypeId,
        int formularyTiers,
        string? address1,
        string? address2,
        string? city,
        string? state,
        string? zip,
        string? phone,
        int? legacyId = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        return new InsuranceCompany
        {
            Id = Guid.CreateVersion7(),
            Name = name.Trim(),
            InsuranceTypeId = insuranceTypeId,
            FormularyTiers = formularyTiers < 0 ? 0 : formularyTiers,
            Address1 = Clean(address1),
            Address2 = Clean(address2),
            City = Clean(city),
            State = Clean(state),
            Zip = Clean(zip),
            Phone = Clean(phone),
            IsActive = true,
            LegacyId = legacyId,
            CreatedAtUtc = DateTime.UtcNow
        };
    }

    public void Update(
        string name,
        Guid? insuranceTypeId,
        int formularyTiers,
        string? address1,
        string? address2,
        string? city,
        string? state,
        string? zip,
        string? phone,
        bool isActive)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        Name = name.Trim();
        InsuranceTypeId = insuranceTypeId;
        FormularyTiers = formularyTiers < 0 ? 0 : formularyTiers;
        Address1 = Clean(address1);
        Address2 = Clean(address2);
        City = Clean(city);
        State = Clean(state);
        Zip = Clean(zip);
        Phone = Clean(phone);
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
