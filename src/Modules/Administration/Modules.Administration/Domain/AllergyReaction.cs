using FSH.Framework.Core.Domain;

namespace FSH.Modules.Administration.Domain;

/// <summary>
/// A curated allergy reaction option (legacy <c>SnomedAssociations</c> rows flagged
/// <c>saIsReaction</c>, joined to the SNOMED description for code + term). The patient chart's
/// allergy dialog multi-picks from this list and appends terms into the allergy's Reaction text.
/// </summary>
public sealed class AllergyReaction : AggregateRoot<int>, ISoftDeletable, IGlobalEntity
{
    public string Term { get; private set; } = default!;
    public string? SnomedCode { get; private set; }
    public bool IsActive { get; private set; }
    public bool IsDeleted { get; private set; }
    public DateTimeOffset? DeletedOnUtc { get; private set; }
    public string? DeletedBy { get; private set; }

    private AllergyReaction() { }

    public static AllergyReaction Create(string term, string? snomedCode = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(term);
        return new AllergyReaction { Term = term.Trim(), SnomedCode = snomedCode?.Trim(), IsActive = true };
    }

    public void Update(string term, bool isActive, string? snomedCode = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(term);
        Term = term.Trim();
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
