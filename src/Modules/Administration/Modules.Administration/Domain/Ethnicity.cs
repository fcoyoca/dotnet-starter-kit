using FSH.Framework.Core.Domain;

namespace FSH.Modules.Administration.Domain;

public sealed class Ethnicity : AggregateRoot<int>, ISoftDeletable, IGlobalEntity
{
    public string Name { get; private set; } = default!;
    public bool IsActive { get; private set; }
    public bool IsDeleted { get; private set; }
    public DateTimeOffset? DeletedOnUtc { get; private set; }
    public string? DeletedBy { get; private set; }

    private Ethnicity() { }

    public static Ethnicity Create(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        return new Ethnicity { Name = name.Trim(), IsActive = true };
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
