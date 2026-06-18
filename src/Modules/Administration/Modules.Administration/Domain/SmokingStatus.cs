using FSH.Framework.Core.Domain;

namespace FSH.Modules.Administration.Domain;

public sealed class SmokingStatus : AggregateRoot<int>, ISoftDeletable, IGlobalEntity
{
    public string Name { get; private set; } = default!;
    public string? SnomedCode { get; private set; }
    public bool IsActive { get; private set; }
    public bool IsDeleted { get; private set; }
    public DateTimeOffset? DeletedOnUtc { get; private set; }
    public string? DeletedBy { get; private set; }

    private SmokingStatus() { }

    public static SmokingStatus Create(string name, string? snomedCode = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        return new SmokingStatus { Name = name.Trim(), SnomedCode = snomedCode?.Trim(), IsActive = true };
    }

    public void Update(string name, bool isActive, string? snomedCode = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        Name = name.Trim();
        IsActive = isActive;
        SnomedCode = snomedCode?.Trim();
    }

    public void Delete(string? deletedBy)
    {
        IsDeleted = true;
        DeletedOnUtc = DateTimeOffset.UtcNow;
        DeletedBy = deletedBy;
    }
}
