using FSH.Framework.Core.Domain;
using FSH.Modules.Patient.Contracts.v1.SuperBills;

namespace FSH.Modules.Patient.Domain;

/// <summary>
/// The super bill of one report (legacy <c>SuperBills</c> — <c>sbReportID</c>/<c>sbBilled</c>/
/// <c>sbBilledDate</c>): the "Procedures Performed" set. One per report (unique <see cref="ReportId"/>).
/// Saving replaces the whole procedure set in one transaction — the C# equivalent of the legacy
/// delete-all-then-reinsert stored procs. <see cref="IsBilled"/>/<see cref="BilledDateUtc"/> stay
/// unset until a billing-provider module (subscribing to <c>SuperBillSavedIntegrationEvent</c>) lands.
/// </summary>
public sealed class SuperBill : AggregateRoot<Guid>
{
    public Guid ReportId { get; private set; }
    public Guid PatientId { get; private set; }

    /// <summary>
    /// The insurance type this bill was priced/billed under, snapshotted at save (not an FK —
    /// insurance types live in the Administration module). Captured so the report's Procedures
    /// Performed stays correct after the patient's insurance changes, matching the Code/Charge
    /// snapshots on <see cref="SuperBillProcedure"/>. Null for legacy rows and self-pay saves.
    /// </summary>
    public Guid? InsuranceTypeId { get; private set; }
    public bool IsBilled { get; private set; }
#pragma warning disable S1144 // Will be set by future billing module subscribing to SuperBillSavedIntegrationEvent
    public DateTime? BilledDateUtc { get; private set; }
#pragma warning restore S1144
    public DateTime CreatedAtUtc { get; private set; }
    public DateTime? UpdatedAtUtc { get; private set; }

    public IReadOnlyList<SuperBillProcedure> Procedures => _procedures.AsReadOnly();
    private readonly List<SuperBillProcedure> _procedures = [];

    private SuperBill() { }

    public static SuperBill Create(Guid reportId, Guid patientId, Guid? insuranceTypeId = null)
    {
        if (reportId == Guid.Empty)
        {
            throw new ArgumentException("Report id is required.", nameof(reportId));
        }

        if (patientId == Guid.Empty)
        {
            throw new ArgumentException("Patient id is required.", nameof(patientId));
        }

        return new SuperBill
        {
            Id = Guid.CreateVersion7(),
            ReportId = reportId,
            PatientId = patientId,
            InsuranceTypeId = insuranceTypeId,
            IsBilled = false,
            CreatedAtUtc = DateTime.UtcNow,
        };
    }

    /// <summary>
    /// Sets the insurance type this bill was priced under (from the picker selection at save).
    /// Reflects the most recent save; freezing on signed/billed is deferred until a billing-provider
    /// module lands — see docs/superpowers/plans/2026-07-15-superbill-insurance-snapshot.md (Task 9).
    /// </summary>
    public void SetInsuranceType(Guid? insuranceTypeId)
    {
        InsuranceTypeId = insuranceTypeId;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    /// <summary>Replaces the whole procedure set (legacy <c>SuperBillProcedures_Set_Multi</c> semantics).</summary>
    public void ReplaceProcedures(IReadOnlyList<ReportProcedureItem> procedures)
    {
        ArgumentNullException.ThrowIfNull(procedures);

        // Positional in-place reconcile rather than Clear()+re-add: duplicate procedure codes are
        // allowed and each row owns a nested diagnostics collection, so match by position
        // (DisplayOrder). Replacing the tracked collection with freshly client-keyed rows makes EF
        // issue phantom UPDATEs against non-existent keys on a loaded bill → DbUpdateConcurrencyException.
        // Keep overlapping rows (real keys → correct UPDATE), add the tail, remove the surplus.
        List<SuperBillProcedure> existing = _procedures.OrderBy(p => p.DisplayOrder).ToList();
        int overlap = Math.Min(existing.Count, procedures.Count);

        for (int i = 0; i < overlap; i++)
        {
            ReportProcedureItem item = procedures[i];
            existing[i].Update(item.ProcedureCodeId, item.Code, item.Description, item.Charge, i, item.DiagnosticIds);
        }

        for (int i = overlap; i < procedures.Count; i++)
        {
            ReportProcedureItem item = procedures[i];
            _procedures.Add(SuperBillProcedure.Create(
                Id, item.ProcedureCodeId, item.Code, item.Description, item.Charge, i, item.DiagnosticIds));
        }

        for (int i = overlap; i < existing.Count; i++)
        {
            _procedures.Remove(existing[i]);
        }

        UpdatedAtUtc = DateTime.UtcNow;
    }
}
