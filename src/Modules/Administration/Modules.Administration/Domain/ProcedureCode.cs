using FSH.Framework.Core.Domain;

namespace FSH.Modules.Administration.Domain;

/// <summary>
/// A procedure (CPT/HCPCS) code (legacy <c>ProcedureCodes</c> — <c>pcCode</c>/<c>pcName</c>/<c>pcDescription</c>/
/// <c>pcMacroText</c>/<c>pcCodeSource</c>). Tenant-scoped — not <see cref="IGlobalEntity"/>, so <c>BaseDbContext</c>
/// applies the per-tenant filter. Optionally belongs to a <see cref="ProcedureCategory"/>. Legacy Kareo modifiers
/// and the insurance-type price association (<c>ItpPrice</c>) are deferred to a later sprint.
/// </summary>
public sealed class ProcedureCode : AggregateRoot<Guid>, ISoftDeletable
{
    public string Code { get; private set; } = default!;
    public string? Name { get; private set; }
    public string? Description { get; private set; }

    /// <summary>Optional FK to a <see cref="ProcedureCategory"/> in this module (same DbContext).</summary>
    public Guid? ProcedureCategoryId { get; private set; }

    /// <summary>Legacy <c>pcCodeSource</c> — e.g. CPT, HCPCS, custom.</summary>
    public string? CodeSource { get; private set; }

    /// <summary>Boilerplate note text inserted when this code is selected (legacy <c>pcMacroText</c>).</summary>
    public string? MacroText { get; private set; }

    public bool IsActive { get; private set; }

    /// <summary>Legacy <c>pcID</c> of the source record; null for native records.</summary>
    public int? LegacyId { get; private set; }

    public DateTime CreatedAtUtc { get; private set; }
    public DateTime? UpdatedAtUtc { get; private set; }

    public bool IsDeleted { get; private set; }
    public DateTimeOffset? DeletedOnUtc { get; private set; }
    public string? DeletedBy { get; private set; }

    private ProcedureCode() { }

    public static ProcedureCode Create(
        string code,
        string? name,
        string? description,
        Guid? procedureCategoryId,
        string? codeSource,
        string? macroText,
        int? legacyId = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(code);

        return new ProcedureCode
        {
            Id = Guid.CreateVersion7(),
            Code = code.Trim(),
            Name = Clean(name),
            Description = Clean(description),
            ProcedureCategoryId = procedureCategoryId,
            CodeSource = Clean(codeSource),
            MacroText = Clean(macroText),
            IsActive = true,
            LegacyId = legacyId,
            CreatedAtUtc = DateTime.UtcNow
        };
    }

    public void Update(
        string code,
        string? name,
        string? description,
        Guid? procedureCategoryId,
        string? codeSource,
        string? macroText,
        bool isActive)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(code);

        Code = code.Trim();
        Name = Clean(name);
        Description = Clean(description);
        ProcedureCategoryId = procedureCategoryId;
        CodeSource = Clean(codeSource);
        MacroText = Clean(macroText);
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
