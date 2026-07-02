using FSH.Framework.Core.Domain;

namespace FSH.Modules.Administration.Domain;

/// <summary>
/// A medication dose unit (legacy <c>MedicationUnitTypes</c>, e.g. mg, mL, tablet) used by the
/// medication dialog's dose-unit dropdown.
/// </summary>
public sealed class MedicationDoseUnit : AggregateRoot<int>, ISoftDeletable, IGlobalEntity
{
    public string Name { get; private set; } = default!;
    public bool IsActive { get; private set; }
    public bool IsDeleted { get; private set; }
    public DateTimeOffset? DeletedOnUtc { get; private set; }
    public string? DeletedBy { get; private set; }

    private MedicationDoseUnit() { }

    public static MedicationDoseUnit Create(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        return new MedicationDoseUnit { Name = name.Trim(), IsActive = true };
    }

    public void Update(string name, bool isActive)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        Name = name.Trim();
        IsActive = isActive;
    }

    public void Delete(string? deletedBy)
    {
        IsDeleted = true;
        DeletedOnUtc = DateTimeOffset.UtcNow;
        DeletedBy = deletedBy;
    }
}
