using FSH.Framework.Core.Domain;

namespace FSH.Modules.Claims.Domain;

/// <summary>
/// An insurance claim originating from a saved super bill. Snapshot-only: every field is copied
/// from <c>SuperBillSavedIntegrationEvent</c> — no FK/joins into Patient/Administration. Lifecycle:
/// Draft → Ready → Submitted → Paid | Denied, plus Voided from any non-terminal state. The snapshot
/// is refreshable only while Draft (see <see cref="IsSnapshotEditable"/>); once a biller advances
/// the claim, a later super-bill re-save must not clobber it.
/// </summary>
public sealed class Claim : AggregateRoot<Guid>
{
    private readonly List<ClaimLine> _lines = new();

    public Guid SuperBillId { get; private set; }
    public Guid ReportId { get; private set; }
    public Guid PatientId { get; private set; }
    public Guid? InsuranceTypeId { get; private set; }
    public bool IsBilled { get; private set; }
    public ClaimStatus Status { get; private set; }
    public decimal TotalCharge { get; private set; }
    public string? ControlNumber { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public DateTime? UpdatedAtUtc { get; private set; }
    public DateTime? SubmittedAtUtc { get; private set; }
    public DateTime? ResolvedAtUtc { get; private set; }

    public IReadOnlyList<ClaimLine> Lines => _lines;

    /// <summary>The snapshot may be refreshed from a re-saved super bill only while Draft.</summary>
    public bool IsSnapshotEditable => Status == ClaimStatus.Draft;

    private Claim() { }

    public static Claim CreateFromSuperBill(
        Guid superBillId, Guid reportId, Guid patientId, Guid? insuranceTypeId, bool isBilled,
        IEnumerable<ClaimLine> lines)
    {
        if (superBillId == Guid.Empty)
        {
            throw new ArgumentException("SuperBillId is required.", nameof(superBillId));
        }

        var claim = new Claim
        {
            Id = Guid.CreateVersion7(),
            SuperBillId = superBillId,
            ReportId = reportId,
            PatientId = patientId,
            InsuranceTypeId = insuranceTypeId,
            IsBilled = isBilled,
            Status = ClaimStatus.Draft,
            CreatedAtUtc = DateTime.UtcNow,
        };
        claim.ReplaceLines(lines);
        return claim;
    }

    /// <summary>Refresh the snapshot from a re-saved super bill. No-op guard belongs to the caller
    /// (the event handler) which checks <see cref="IsSnapshotEditable"/> first.</summary>
    public void RefreshSnapshot(Guid? insuranceTypeId, bool isBilled, IEnumerable<ClaimLine> lines)
    {
        InsuranceTypeId = insuranceTypeId;
        IsBilled = isBilled;
        ReplaceLines(lines);
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void MarkReady()
    {
        RequireStatus(ClaimStatus.Draft, "mark ready");
        Status = ClaimStatus.Ready;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void Submit(string controlNumber)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(controlNumber);
        RequireStatus(ClaimStatus.Ready, "submit");
        Status = ClaimStatus.Submitted;
        ControlNumber = controlNumber;
        SubmittedAtUtc = DateTime.UtcNow;
        UpdatedAtUtc = SubmittedAtUtc;
    }

    public void MarkPaid()
    {
        RequireStatus(ClaimStatus.Submitted, "mark paid");
        Status = ClaimStatus.Paid;
        ResolvedAtUtc = DateTime.UtcNow;
        UpdatedAtUtc = ResolvedAtUtc;
    }

    public void MarkDenied()
    {
        RequireStatus(ClaimStatus.Submitted, "mark denied");
        Status = ClaimStatus.Denied;
        ResolvedAtUtc = DateTime.UtcNow;
        UpdatedAtUtc = ResolvedAtUtc;
    }

    public void Void()
    {
        if (Status is ClaimStatus.Voided)
        {
            return; // idempotent
        }
        if (Status is ClaimStatus.Paid or ClaimStatus.Denied)
        {
            throw new InvalidOperationException($"Cannot void a claim in status {Status}.");
        }
        Status = ClaimStatus.Voided;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    private void ReplaceLines(IEnumerable<ClaimLine> lines)
    {
        ArgumentNullException.ThrowIfNull(lines);
        _lines.Clear();
        foreach (var line in lines)
        {
            _lines.Add(ClaimLine.Create(Id, line.ProcedureCodeId, line.Code, line.Description, line.Charge, line.DiagnosticIds));
        }
        TotalCharge = _lines.Sum(l => l.Charge);
    }

    private void RequireStatus(ClaimStatus expected, string action)
    {
        if (Status != expected)
        {
            throw new InvalidOperationException($"Cannot {action}: claim status is {Status}, expected {expected}.");
        }
    }
}
