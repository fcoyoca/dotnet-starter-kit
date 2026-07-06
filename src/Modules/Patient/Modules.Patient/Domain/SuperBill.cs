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
    public bool IsBilled { get; private set; }
#pragma warning disable S1144 // Will be set by future billing module subscribing to SuperBillSavedIntegrationEvent
    public DateTime? BilledDateUtc { get; private set; }
#pragma warning restore S1144
    public DateTime CreatedAtUtc { get; private set; }
    public DateTime? UpdatedAtUtc { get; private set; }

    public IReadOnlyList<SuperBillProcedure> Procedures => _procedures.AsReadOnly();
    private readonly List<SuperBillProcedure> _procedures = [];

    private SuperBill() { }

    public static SuperBill Create(Guid reportId, Guid patientId)
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
            IsBilled = false,
            CreatedAtUtc = DateTime.UtcNow,
        };
    }

    /// <summary>Replaces the whole procedure set (legacy <c>SuperBillProcedures_Set_Multi</c> semantics).</summary>
    public void ReplaceProcedures(IReadOnlyList<ReportProcedureItem> procedures)
    {
        ArgumentNullException.ThrowIfNull(procedures);

        _procedures.Clear();
        for (int i = 0; i < procedures.Count; i++)
        {
            ReportProcedureItem item = procedures[i];
            _procedures.Add(SuperBillProcedure.Create(
                Id, item.ProcedureCodeId, item.Code, item.Description, item.Charge, i, item.DiagnosticIds));
        }

        UpdatedAtUtc = DateTime.UtcNow;
    }
}
