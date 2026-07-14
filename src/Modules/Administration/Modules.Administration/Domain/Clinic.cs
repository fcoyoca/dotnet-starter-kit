using FSH.Framework.Core.Domain;
using FSH.Modules.Administration.Contracts.Dtos;

namespace FSH.Modules.Administration.Domain;

/// <summary>
/// A clinic (physical location) belonging to a tenant. Tenant-scoped business data — unlike the
/// reference lookups in this module it is NOT <see cref="IGlobalEntity"/>, so <c>BaseDbContext</c>
/// applies the Finbuckle per-tenant query filter automatically and each tenant owns its own clinics.
/// </summary>
public sealed class Clinic : AggregateRoot<Guid>, ISoftDeletable
{
    public string Code { get; private set; } = default!;
    public string Name { get; private set; } = default!;
    public string Address1 { get; private set; } = default!;
    public string? Address2 { get; private set; }
    public string City { get; private set; } = default!;
    public string State { get; private set; } = default!;
    public string Zip { get; private set; } = default!;
    public string? Phone { get; private set; }
    public bool IsActive { get; private set; }

    /// <summary>
    /// IANA timezone of this clinic (e.g. <c>America/New_York</c>), used to render the scheduler day and
    /// appointment times in the clinic's local wall-clock. Defaults to <c>UTC</c>.
    /// </summary>
    public string TimeZoneId { get; private set; } = "UTC";

    /// <summary>
    /// Page orientation for this clinic's exported patient-report PDFs (legacy
    /// <c>PRINT_ORIENTATION</c>). Defaults to <see cref="PrintOrientation.Portrait"/>.
    /// </summary>
    public PrintOrientation PrintOrientation { get; private set; } = PrintOrientation.Portrait;

    /// <summary>
    /// Surrogate key (<c>cID</c>) of the source record in the legacy BackChart/Bronston database.
    /// Null for clinics created natively. Preserved so a later data-migration import can key
    /// related records back to the original clinic.
    /// </summary>
    public int? LegacyId { get; private set; }

    public DateTime CreatedAtUtc { get; private set; }
    public DateTime? UpdatedAtUtc { get; private set; }

    public bool IsDeleted { get; private set; }
    public DateTimeOffset? DeletedOnUtc { get; private set; }
    public string? DeletedBy { get; private set; }

    private Clinic() { }

    public static Clinic Create(
        string code,
        string name,
        string address1,
        string? address2,
        string city,
        string state,
        string zip,
        string? phone,
        int? legacyId = null,
        string? timeZoneId = null,
        PrintOrientation printOrientation = PrintOrientation.Portrait)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(code);
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentException.ThrowIfNullOrWhiteSpace(address1);
        ArgumentException.ThrowIfNullOrWhiteSpace(city);
        ArgumentException.ThrowIfNullOrWhiteSpace(state);
        ArgumentException.ThrowIfNullOrWhiteSpace(zip);

        return new Clinic
        {
            Id = Guid.CreateVersion7(),
            Code = code.Trim(),
            Name = name.Trim(),
            Address1 = address1.Trim(),
            Address2 = string.IsNullOrWhiteSpace(address2) ? null : address2.Trim(),
            City = city.Trim(),
            State = state.Trim(),
            Zip = zip.Trim(),
            Phone = string.IsNullOrWhiteSpace(phone) ? null : phone.Trim(),
            IsActive = true,
            LegacyId = legacyId,
            TimeZoneId = string.IsNullOrWhiteSpace(timeZoneId) ? "UTC" : timeZoneId.Trim(),
            PrintOrientation = printOrientation,
            CreatedAtUtc = DateTime.UtcNow
        };
    }

    public void Update(
        string code,
        string name,
        string address1,
        string? address2,
        string city,
        string state,
        string zip,
        string? phone,
        bool isActive,
        string? timeZoneId = null,
        PrintOrientation printOrientation = PrintOrientation.Portrait)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(code);
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentException.ThrowIfNullOrWhiteSpace(address1);
        ArgumentException.ThrowIfNullOrWhiteSpace(city);
        ArgumentException.ThrowIfNullOrWhiteSpace(state);
        ArgumentException.ThrowIfNullOrWhiteSpace(zip);

        Code = code.Trim();
        Name = name.Trim();
        Address1 = address1.Trim();
        Address2 = string.IsNullOrWhiteSpace(address2) ? null : address2.Trim();
        City = city.Trim();
        State = state.Trim();
        Zip = zip.Trim();
        Phone = string.IsNullOrWhiteSpace(phone) ? null : phone.Trim();
        IsActive = isActive;
        TimeZoneId = string.IsNullOrWhiteSpace(timeZoneId) ? "UTC" : timeZoneId.Trim();
        PrintOrientation = printOrientation;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void Delete(string? deletedBy)
    {
        IsDeleted = true;
        DeletedOnUtc = DateTimeOffset.UtcNow;
        DeletedBy = deletedBy;
    }
}
