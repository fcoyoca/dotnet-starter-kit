using FSH.Framework.Core.Domain;

namespace FSH.Modules.Administration.Domain;

/// <summary>
/// A type of appointment that can be scheduled (legacy <c>AppointmentTypes</c> — <c>appttypeName</c>/
/// <c>appttypeColor</c>/<c>appttypeDefaultDuration</c>), e.g. "Wellness", "X-ray". Tenant-scoped CRUD lookup
/// (legacy has no clinic key — appointment types are shared across a tenant's clinics). Soft-deleted so a type in
/// use can be hidden without breaking historic appointments.
/// </summary>
public sealed class AppointmentType : AggregateRoot<Guid>, ISoftDeletable
{
    public string Name { get; private set; } = default!;

    /// <summary>Calendar colour as a hex string (legacy <c>appttypeColor</c>), e.g. <c>#3366cc</c>.</summary>
    public string? Color { get; private set; }

    /// <summary>Default appointment length in minutes (legacy <c>appttypeDefaultDuration</c>).</summary>
    public int DefaultDurationMinutes { get; private set; }

    public int DisplayOrder { get; private set; }
    public bool IsActive { get; private set; }

    /// <summary>Legacy <c>appttypeID</c> of the source record; null for native records.</summary>
    public int? LegacyId { get; private set; }

    public DateTime CreatedAtUtc { get; private set; }
    public DateTime? UpdatedAtUtc { get; private set; }

    public bool IsDeleted { get; private set; }
    public DateTimeOffset? DeletedOnUtc { get; private set; }
    public string? DeletedBy { get; private set; }

    private AppointmentType() { }

    public static AppointmentType Create(
        string name,
        string? color,
        int defaultDurationMinutes,
        int displayOrder = 0,
        int? legacyId = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        return new AppointmentType
        {
            Id = Guid.CreateVersion7(),
            Name = name.Trim(),
            Color = Trim(color),
            DefaultDurationMinutes = defaultDurationMinutes,
            DisplayOrder = displayOrder,
            IsActive = true,
            LegacyId = legacyId,
            CreatedAtUtc = DateTime.UtcNow,
        };
    }

    public void Update(string name, string? color, int defaultDurationMinutes, int displayOrder, bool isActive)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        Name = name.Trim();
        Color = Trim(color);
        DefaultDurationMinutes = defaultDurationMinutes;
        DisplayOrder = displayOrder;
        IsActive = isActive;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void Delete(string? deletedBy)
    {
        IsDeleted = true;
        DeletedOnUtc = DateTimeOffset.UtcNow;
        DeletedBy = deletedBy;
    }

    private static string? Trim(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
