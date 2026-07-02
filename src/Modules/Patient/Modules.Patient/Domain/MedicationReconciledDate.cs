using FSH.Framework.Core.Domain;

namespace FSH.Modules.Patient.Domain;

/// <summary>
/// A record that a clinician reconciled the patient's medication list on a given date
/// (legacy <c>MedicationReconciledDates</c>, surfaced in the "Dates Reconciled" dialog).
/// Append-only history — no update or delete.
/// </summary>
public sealed class MedicationReconciledDate : AggregateRoot<Guid>
{
    public Guid PatientId { get; private set; }
    public DateTime ReconciledOn { get; private set; }
    public string? CreatedByUserId { get; private set; }
    public string? CreatedByName { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }

    private MedicationReconciledDate() { }

    public static MedicationReconciledDate Create(
        Guid patientId,
        DateTime reconciledOn,
        string? createdByUserId,
        string? createdByName)
    {
        return new MedicationReconciledDate
        {
            Id = Guid.CreateVersion7(),
            PatientId = patientId,
            ReconciledOn = reconciledOn.Date,
            CreatedByUserId = createdByUserId,
            CreatedByName = createdByName,
            CreatedAtUtc = DateTime.UtcNow
        };
    }
}
