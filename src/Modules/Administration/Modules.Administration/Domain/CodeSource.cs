using FSH.Framework.Core.Domain;

namespace FSH.Modules.Administration.Domain;

/// <summary>
/// Reference list of procedure code systems (legacy <c>CodeSources</c> — <c>csID</c>/<c>csName</c>), e.g.
/// CPT, HCPCS. Cross-tenant reference data (<see cref="IGlobalEntity"/>) like the other Administration
/// lookups; soft-deleted so a source in use can be hidden without breaking historic references.
/// </summary>
public sealed class CodeSource : AggregateRoot<int>, ISoftDeletable, IGlobalEntity
{
    public string Name { get; private set; } = default!;
    public bool IsActive { get; private set; }
    public bool IsDeleted { get; private set; }
    public DateTimeOffset? DeletedOnUtc { get; private set; }
    public string? DeletedBy { get; private set; }

    private CodeSource() { }

    public static CodeSource Create(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        return new CodeSource { Name = name.Trim(), IsActive = true };
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
