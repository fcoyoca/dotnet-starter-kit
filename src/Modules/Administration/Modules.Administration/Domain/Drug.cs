using FSH.Framework.Core.Domain;

namespace FSH.Modules.Administration.Domain;

/// <summary>
/// A drug concept in the global catalog (legacy RxNorm <c>RXNCONSO</c> subset: RXAUI atom id,
/// RXCUI concept id, STR display name, TTY term type, SAB source vocabulary, CODE source code).
/// Cross-tenant reference data (<see cref="IGlobalEntity"/>) searched by the patient chart's
/// allergy/medication drug pickers; managed via admin CRUD and on-demand RxNav import.
/// Soft-deleted so an in-use drug can be hidden without breaking historic references.
/// </summary>
public sealed class Drug : AggregateRoot<int>, ISoftDeletable, IGlobalEntity
{
    public string Name { get; private set; } = default!;
    public string? RxAui { get; private set; }
    public string? RxCui { get; private set; }
    public string? Tty { get; private set; }
    public string? Sab { get; private set; }
    public string? Code { get; private set; }
    public bool IsActive { get; private set; }

    public DateTime CreatedAtUtc { get; private set; }
    public DateTime? UpdatedAtUtc { get; private set; }

    public bool IsDeleted { get; private set; }
    public DateTimeOffset? DeletedOnUtc { get; private set; }
    public string? DeletedBy { get; private set; }

    private Drug() { }

    public static Drug Create(
        string name,
        string? rxAui = null,
        string? rxCui = null,
        string? tty = null,
        string? sab = null,
        string? code = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        return new Drug
        {
            Name = name.Trim(),
            RxAui = Clean(rxAui),
            RxCui = Clean(rxCui),
            Tty = Clean(tty),
            Sab = Clean(sab),
            Code = Clean(code),
            IsActive = true,
            CreatedAtUtc = DateTime.UtcNow
        };
    }

    public void Update(
        string name,
        string? rxAui,
        string? rxCui,
        string? tty,
        string? sab,
        string? code,
        bool isActive)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        Name = name.Trim();
        RxAui = Clean(rxAui);
        RxCui = Clean(rxCui);
        Tty = Clean(tty);
        Sab = Clean(sab);
        Code = Clean(code);
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
