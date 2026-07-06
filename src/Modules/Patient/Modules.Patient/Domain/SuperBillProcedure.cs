namespace FSH.Modules.Patient.Domain;

/// <summary>
/// One procedure administered in a report's super bill (legacy <c>SuperBillProcedures</c> —
/// <c>sbpProcedureCodeID</c>/<c>sbpCharge</c>). <see cref="Code"/>/<see cref="Description"/> are
/// snapshots taken at save time so saved rows render without a cross-module lookup and stay
/// billing-correct if the catalog changes. Duplicate procedure codes per bill are allowed (legacy).
/// </summary>
public sealed class SuperBillProcedure
{
    public Guid Id { get; private set; }
    public Guid SuperBillId { get; private set; }

    /// <summary>Bare id into the Administration <c>ProcedureCode</c> catalog — no cross-module FK.</summary>
    public Guid ProcedureCodeId { get; private set; }

    public string Code { get; private set; } = default!;
    public string? Description { get; private set; }
    public decimal Charge { get; private set; }
    public int DisplayOrder { get; private set; }

    public IReadOnlyList<SuperBillProcedureDiagnostic> Diagnostics => _diagnostics.AsReadOnly();
    private readonly List<SuperBillProcedureDiagnostic> _diagnostics = [];

    private SuperBillProcedure() { }

    public static SuperBillProcedure Create(
        Guid superBillId,
        Guid procedureCodeId,
        string code,
        string? description,
        decimal charge,
        int displayOrder,
        IReadOnlyList<Guid> diagnosticIds)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(code);
        ArgumentNullException.ThrowIfNull(diagnosticIds);

        var procedure = new SuperBillProcedure
        {
            Id = Guid.CreateVersion7(),
            SuperBillId = superBillId,
            ProcedureCodeId = procedureCodeId,
            Code = code.Trim(),
            Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim(),
            Charge = charge < 0 ? 0 : charge,
            DisplayOrder = displayOrder,
        };

        foreach (Guid diagnosticId in diagnosticIds.Distinct())
        {
            procedure._diagnostics.Add(SuperBillProcedureDiagnostic.Create(procedure.Id, diagnosticId));
        }

        return procedure;
    }
}
